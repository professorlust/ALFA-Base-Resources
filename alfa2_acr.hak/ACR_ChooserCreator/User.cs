﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACR_ChooserCreator
{
    public static class Users
    {
        public static Dictionary<uint, User> TrackedUsers = new Dictionary<uint, User>();

        public static User GetUser(uint userId)
        {
            if(TrackedUsers.Keys.Contains(userId))
            {
                return TrackedUsers[userId];
            }
            else
            {
                User newUser = new User()
                {
                    Id = userId,
                    CurrentCreatureCategory = Navigators.CreatureNavigator.bottomCategory,
                    CurrentItemCategory = Navigators.ItemNavigator.bottomCategory,
                    CurrentLightCategory = Navigators.LightNavigator.bottomCategory,
                    CurrentPlaceableCategory = Navigators.PlaceableNavigator.bottomCategory,
                    CurrentVisualEffectCategory = Navigators.VisualEffectNavigator.bottomCategory,
                    CurrentWaypointCategory = Navigators.WaypointNavigator.bottomCategory,
                    SortingColumn = 1
                };
                TrackedUsers.Add(userId, newUser);
                return newUser;
            }
        }
    }
    public class User
    {
        public uint Id;
        private int _searchNumber;

        public int SearchNumber
        {
            get
            {
                return _searchNumber;
            }
            set
            {
                if (value > 12) _searchNumber = 1;
                else if (value < 1) _searchNumber = 1;
                else _searchNumber = value;
            }
        }

        public ACR_ChooserCreator.ACR_CreatorCommand openCommand;
        public NavigatorCategory CurrentCreatureCategory;
        public NavigatorCategory CurrentLightCategory;
        public NavigatorCategory CurrentPlaceableCategory;
        public NavigatorCategory CurrentItemCategory;
        public NavigatorCategory CurrentVisualEffectCategory;
        public NavigatorCategory CurrentWaypointCategory;
        public int SortingColumn;
    }
}
