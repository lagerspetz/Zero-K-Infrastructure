﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using LobbyClient;
using ZkData;
using System.Data.Entity.SqlServer;

namespace ZeroKWeb.Controllers
{
    public class UsersController: Controller
    {
        //
        // GET: /Users/
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ChangeHideCountry(int accountID, bool hideCountry)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);

            if (hideCountry) acc.Country = "??";
            // TODO reimplement ? Global.Nightwatch.Tas.SetHideCountry(acc.Name, hideCountry);
            db.SaveChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ChangeAccountDeleted(int accountID, bool isDeleted)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);

            if (acc.IsDeleted != isDeleted)
            {
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Account {0} {1} deletion status changed by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), Global.Account.Name));
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - {0} -> {1}", acc.IsDeleted, isDeleted));
                acc.IsDeleted = isDeleted;
            }
            db.SaveChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [Auth(Role = AdminLevel.SuperAdmin)]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePermissions(int accountID, bool zkAdmin, bool vpnException)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);
            Account adminAcc = Global.Account;
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Permissions changed for {0} {1} by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), adminAcc.Name));

            var curAdmin = acc.AdminLevel > AdminLevel.None;
            if (curAdmin != zkAdmin)
            {
                //reset chat priviledges to 2 if removing adminhood; remove NW subsciption to admin channel
                // FIXME needs to also terminate forbidden clan/faction subscriptions
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - Moderator status: {0} -> {1}", curAdmin, zkAdmin));
                acc.AdminLevel = zkAdmin ? AdminLevel.Moderator : AdminLevel.None;
                
            }
            if (acc.HasVpnException != vpnException)
            {
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - VPN exception: {0} -> {1}", acc.HasVpnException, vpnException));
                acc.HasVpnException = vpnException;
            }
            db.SaveChanges();

            Global.Server.PublishAccountUpdate(acc);
            
            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ChangeElo(int accountID, int eloweight, int eloweight1v1)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);
            Account adminAcc = Global.Account;
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Fake elo malus changed for {0} {1} by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), adminAcc.Name));
            if (acc.EloWeight != eloweight) {
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - Team Elo Weight: {0} -> {1}", acc.EloWeight, eloweight));
                acc.EloWeight = eloweight;
            }
            if (acc.EloMmWeight != eloweight1v1) {
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - 1v1 Elo Weight: {0} -> {1}", acc.EloMmWeight, eloweight1v1));
                acc.EloMmWeight = eloweight1v1;
            }
            db.SaveChanges();

            Global.Server.PublishAccountUpdate(acc);
            
            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult AdminUserDetail(int id)
        {
            var db = new ZkDataContext();
            var user = db.Accounts.Find(id);
            return View("AdminUserDetail", user);
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ReportLog ()
        {
            return View("ReportLog");
        }


        public ActionResult Detail(string id)
        {
            var db = new ZkDataContext();

            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);
            return View("UserDetail", user);
        }


        public class UsersIndexModel
        {
            public string Name { get; set; }
            public string IP { get; set; }
            public string Country { get; set; }
            public long? UserID { get; set; }
            public DateTime? RegisteredFrom { get; set; }
            public DateTime? RegisteredTo { get; set; }

            public DateTime? LastLoginFrom { get; set; }
            public DateTime? LastLoginTo { get; set; }

            public bool IsAdmin { get; set; }
            public IQueryable<Account> Data;
        }


        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Index(UsersIndexModel model) {
            model = model ?? new UsersIndexModel();
            var db = new ZkDataContext();
            var ret = db.Accounts.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(model.Name))
            {
                var termLower = model.Name.ToLower();
                ret = ret.Where(x => x.Name.ToLower().Contains(termLower) || x.SteamName.Contains(model.Name));
            }
            if (Global.IsModerator)
            {
                if (!string.IsNullOrEmpty(model.IP)) ret = ret.Where(x => x.AccountIPs.Any(y => y.IP == model.IP));
                if (model.UserID.HasValue) ret = ret.Where(x => x.AccountUserIDs.Any(y => y.UserID == model.UserID));
            }
            if (!string.IsNullOrEmpty(model.Country))
            {
                var termLower = model.Country.ToLower();
                ret = ret.Where(x => x.Country.ToLower().Contains(termLower));
            }

            if (model.RegisteredFrom.HasValue) ret = ret.Where(x => x.FirstLogin >= model.RegisteredFrom);
            if (model.RegisteredTo.HasValue) ret = ret.Where(x => x.FirstLogin <= model.RegisteredTo);

            if (model.LastLoginFrom.HasValue) ret = ret.Where(x => x.LastLogin >= model.LastLoginFrom);
            if (model.LastLoginTo.HasValue) ret = ret.Where(x => x.LastLogin <= model.LastLoginTo);

            if (model.IsAdmin) ret = ret.Where(x => x.AdminLevel >= AdminLevel.Moderator);

            model.Data = ret.OrderByDescending(x=>x.AccountID);

            return View("UsersIndex", model);
        }


        /// <summary>
        /// Get user detail page by username or <see cref="Account"/> ID
        /// </summary>
        /// <param name="id">Name or ID</param>
        public ActionResult LobbyDetail(string id)
        {
            var db = new ZkDataContext();
            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);

            return View("UserDetail", user);
        }

        const int MaxBanHours = 24 * 36525;   // 100 years

        /// <summary>
        /// Apply a <see cref="Punishment"/> (e.g. bans) and notifies lobby server
        /// </summary>
        /// <param name="accountID"><see cref="Account"/> ID of the person being punished</param>
        /// <param name="reason">Displayed reason for the penalty</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Punish(int accountID,
                                   string reason,
                                   bool deleteXP,
                                   bool deleteInfluence,
                                   bool banMute,
                                   bool banCommanders,
                                   bool banSite,
                                   bool banLobby,
                                   bool banUnlocks,
                                   bool banSpecChat,
                                   bool banForum,
                                   bool setRightsToZero,            
                                   string banIP,
                                   long? banUserID,
                                   double banHours)
        {
            ZkDataContext db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);

            if (banHours > MaxBanHours) banHours = MaxBanHours; // todo show some notification 

            Punishment punishment = new Punishment
                             {
                                 Time = DateTime.UtcNow,
                                 Reason = reason,
                                 BanMute = banMute,
                                 BanCommanders = banCommanders,
                                 BanSite = banSite,
                                 BanLobby = banLobby,
                                 BanExpires = DateTime.UtcNow.AddHours(banHours),
                                 BanUnlocks = banUnlocks,
                                 BanSpecChat = banSpecChat,
                                 BanIP = banIP,
                                 BanForum = banForum,
                                 DeleteXP = deleteXP,
                                 DeleteInfluence = deleteInfluence,
                                 CreatedAccountID = Global.AccountID,
                                 UserID = banUserID
                             };
            acc.PunishmentsByAccountID.Add(punishment);
            db.SaveChanges();

            // notify lobby of changes and post log message
            try
            {
                if (banLobby == true) Global.Server.KickFromServer(Global.Account.Name, acc.Name, reason);
                if (banMute == true) Global.Server.PublishAccountUpdate(acc);

                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("New penalty for {0} {1}  ", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http")));
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Reason: {0} ", reason));
                Global.Server.GhostPm(acc.Name, string.Format("Your account has received moderator action: {0}", reason));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, ex.ToString());
            }
            return RedirectToAction("Detail", new { id = accountID });
        }

        [Auth]
        public ActionResult ReportToAdmin(int id)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(id);
            return View("ReportToAdmin", acc);
        }

        public ActionResult ReportToAdminFromLobby(string id)
        {
            var db = new ZkDataContext();
            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);

            return View("ReportToAdmin", user);
        }

        [Auth]
        [ValidateInput(false)]
        public ActionResult ReportToAdminSubmit(int accountID, string text)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(accountID);
            
            db.AbuseReports.InsertOnSubmit(new AbuseReport()
                                           {
                                               AccountID = acc.AccountID,
                                               ReporterAccountID = Global.AccountID,
                                               Time = DateTime.UtcNow,
                                               Text = text
                                           });
            db.SaveChanges();

            string str;
            if (Global.AccountID != accountID)
                str = string.Format("{0} {1} reports abuse by {2} {3} : {4}", Global.Account.Name, 
                    Url.Action("Detail", "Users", new { id = Global.AccountID }, "http"), 
                    acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), 
                    text);
            else
                str = string.Format("{0} {1} contacts admins : {2}", Global.Account.Name, 
                    Url.Action("Detail", "Users", new { id = Global.AccountID }, "http"), text);

            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str, isRing:true);
            return Content("Thank you. Your issue was reported. Moderators will now look into it.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult RemovePunishment(int punishmentID) {
            var db = new ZkDataContext();
            var todel = db.Punishments.First(x => x.PunishmentID == punishmentID);

            Account acc = todel.AccountByAccountID;
            string punisherName = "<unknown>";
            string reason = todel.Reason ?? "<unknown reason>";
            if (todel.CreatedAccountID != null)
            {
                Account adminAcc = db.Accounts.Find((int)todel.CreatedAccountID);
                if (adminAcc != null) punisherName = adminAcc.Name;
            }

            db.Punishments.DeleteOnSubmit(todel);
            db.SaveChanges();

            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{0} removed a punishment given by {1} ", Global.Account.Name, punisherName));
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("to {0} for: {1} ", acc.Name, todel.Reason));

            return RedirectToAction("Detail", "Users", new { id = todel.AccountID });
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult MassBan()
        {
            return View("MassBan");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult MassBanSubmit(string name, int startIndex, int endIndex, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
        {
            ZkDataContext db = new ZkDataContext();
            int? firstAccID = null;
            if (banHours > MaxBanHours) banHours = MaxBanHours;
            for (int i = startIndex; i <= endIndex; i++)
            {
                Account acc = db.Accounts.FirstOrDefault(x => x.Name == name + i);
                if (acc != null)
                {
                    firstAccID = firstAccID ?? acc.AccountID;
                    long? userID = banID ? (uint?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
                    string userIP = banIP ? acc.AccountIPs.OrderByDescending(x => x.LastLogin).FirstOrDefault().IP : null;
                    System.Console.WriteLine(acc.Name, userID, userIP);
                    Punishment punishment = new Punishment
                    {
                        Time = DateTime.UtcNow,
                        Reason = reason,
                        BanSite = banSite,
                        BanLobby = banLobby,
                        BanExpires = DateTime.UtcNow.AddHours(banHours),
                        BanIP = userIP,
                        CreatedAccountID = Global.AccountID,
                        UserID = userID,
                    };
                    acc.PunishmentsByAccountID.Add(punishment);

                    try
                    {
                        Global.Server.KickFromServer(Global.Account.Name, acc.Name, reason);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
            db.SaveChanges();
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Mass ban executed by {4} for user series {0} ({1} - {2}): {3}",
                name, startIndex, endIndex, Url.Action("Detail", "Users", new { id = firstAccID }, "http"), Global.Account.Name));

            return Index(new UsersIndexModel() {Name = name});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult MassBanByUserIDSubmit(long userID, double? maxAge, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
        {
            ZkDataContext db = new ZkDataContext();
            if (banHours > MaxBanHours) banHours = MaxBanHours;
            DateTime firstLoginAfter = maxAge != null? DateTime.UtcNow.AddHours(-(double)maxAge) : DateTime.MinValue; 
            foreach (Account acc in db.Accounts.Where(x => x.AccountUserIDs.Any(y => y.UserID == userID) && (maxAge == null || x.FirstLogin > firstLoginAfter) ))
            {
                long? punishmentUserID = banID ? (uint?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
                string userIP = banIP ? acc.AccountIPs.OrderByDescending(x => x.LastLogin).FirstOrDefault().IP : null;
                System.Console.WriteLine(acc.Name, userID, userIP);
                Punishment punishment = new Punishment
                {
                    Time = DateTime.UtcNow,
                    Reason = reason,
                    BanSite = banSite,
                    BanLobby = banLobby,
                    BanExpires = DateTime.UtcNow.AddHours(banHours),
                    BanIP = userIP,
                    CreatedAccountID = Global.AccountID,
                    UserID = punishmentUserID,
                };
                acc.PunishmentsByAccountID.Add(punishment);

                try
                {
                    Global.Server.KickFromServer(Global.Account.Name, acc.Name, reason);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            db.SaveChanges();
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Mass ban executed by {2} for userID {0} (max age {1})",
                userID, maxAge, Global.Account.Name));

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult SetPassword(int accountID, string newPassword)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(accountID);
            if (acc.AdminLevel > AdminLevel.None) return Content("Cannot set password on this user");
            acc.SetPasswordPlain(newPassword);
            if (!string.IsNullOrEmpty(newPassword)) acc.SteamID = null;
            db.SaveChanges();
            return Content(string.Format("{0} password set to {1}", acc.Name, newPassword));
        }

        [HttpPost]
        [Auth]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string newPassword2)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(Global.AccountID);
            if (string.IsNullOrEmpty(acc.PasswordBcrypt)) return Content("Your account is password-less, use steam");
            if (AuthServiceClient.VerifyAccountPlain(acc.Name, oldPassword) == null)
            {
                Trace.TraceWarning("Failed password check for {0} on attempted password change", Global.Account.Name);
                Global.Server.LoginChecker.LogIpFailure(Request.UserHostAddress);
                return Content("Invalid password");
            } 
            if (newPassword != newPassword2) return Content("New passwords do not match");
            if (string.IsNullOrWhiteSpace(newPassword)) return Content("New password cannot be blank");
            acc.SetPasswordPlain(newPassword);
            db.SaveChanges();
            //return Content("Old: " + oldPassword + "; new: " + newPassword);
            return RedirectToAction("Logout", "Home");
        }
    }
}
