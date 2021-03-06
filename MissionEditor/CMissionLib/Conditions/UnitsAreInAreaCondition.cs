﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitsAreInAreaCondition : Condition
	{
		ObservableCollection<Area> areas = new ObservableCollection<Area>();
		ObservableCollection<string> groups = new ObservableCollection<string>();
		double number = 1;
		ObservableCollection<Player> players = new ObservableCollection<Player>();

		public UnitsAreInAreaCondition()
			: base() {}

		[DataMember]
		public ObservableCollection<Player> Players
		{
			get { return players; }
			set
			{
				players = value;
				RaisePropertyChanged("Players");
			}
		}

		[DataMember]
		public ObservableCollection<Area> Areas
		{
			get { return areas; }
			set
			{
				areas = value;
				RaisePropertyChanged("Areas");
			}
		}

		[DataMember]
		public ObservableCollection<string> Groups
		{
			get { return groups; }
			set
			{
				groups = value;
				RaisePropertyChanged("Groups");
			}
		}

		[DataMember]
		public double Number
		{
			get { return number; }
			set
			{
				number = value;
				RaisePropertyChanged("Number");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"areas", LuaTable.CreateArray(areas.Select(a => a.GetLuaMap(mission)))},
					{"groups", LuaTable.CreateSet(groups)},
					{"players", LuaTable.CreateArray(players.Select(p => mission.Players.IndexOf(p)))},
					{"number", number}
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Units Are In Area";
		}
	}
}