﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using CLRScriptFramework;
using ALFA;
using NWScript;
using NWScript.ManagedInterfaceLayer.NWScriptManagedInterface;

using NWEffect = NWScript.NWScriptEngineStructure0;
using NWEvent = NWScript.NWScriptEngineStructure1;
using NWLocation = NWScript.NWScriptEngineStructure2;
using NWTalent = NWScript.NWScriptEngineStructure3;
using NWItemProperty = NWScript.NWScriptEngineStructure4;

namespace ACR_ChooserCreator
{
    public partial class ACR_ChooserCreator : CLRScriptBase, IGeneratedScriptProgram
    {
        // Methods and constants for integration with other ACR systems.
        
        // from core ACR stuff.
        const string ACR_REST_TIMER = "ACR_REST_TIMER";
        const string ACR_REST_PRAYER_TIMER = "ACR_REST_PRAYER_TIMER";
        const string ACR_REST_STUDY_TIMER = "ACR_REST_STUDY_TIMER";
        const string ACR_PPS_QUARANTINED = "ACR_PPS_QUARANTINED";
        const string ACR_LOG_VALIDATED = "Validated";
        const string _ACR_PTL_RECORD = "ACR_PTL_RECORD";
        const string _ACR_PTL_PASSPORT = "ACR_PTL_PASSPORT";

        // from reports
        const int PLAYER_REPORT_SHOW_INVENTORY = 1;

        // from traps
        const int TRAP_EVENT_DESPAWN_TRAP = 8;

        ALFA.Database Database;

        public ALFA.Database GetDatabase()
        {
            if (Database == null)
                Database = new ALFA.Database(this);

            return Database;
        }

        public ACR_ChooserCreator([In] NWScriptJITIntrinsics Intrinsics, [In] INWScriptProgram Host)
        {
            InitScript(Intrinsics, Host);
        }

        private ACR_ChooserCreator([In] ACR_ChooserCreator Other)
        {
            InitScript(Other);

            LoadScriptGlobals(Other.SaveScriptGlobals());
        }

        public static Type[] ScriptParameterTypes = { typeof(int), typeof(string) };

        public Int32 ScriptMain([In] object[] ScriptParameters, [In] Int32 DefaultReturnCode)
        {
            bool debug = true;
            int commandNumber = (int)ScriptParameters[0];
            ACR_CreatorCommand command = (ACR_CreatorCommand)commandNumber;
            if (command == ACR_CreatorCommand.ACR_CHOOSERCREATOR_INITIALIZE_LISTS)
            {
                if (OBJECT_SELF == GetModule())
                {
                    ChooserLists.SortLists(this);
                    BackgroundLoader loader = new BackgroundLoader();
                    loader.DoWork += BackgroundLoader.LoadNavigators;
                    loader.RunWorkerAsync();
                    return 0;
                }
                else
                {
                    if (Users.GetUser(OBJECT_SELF).openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_INITIALIZE_LISTS)
                    {
                        Users.GetUser(OBJECT_SELF).openCommand = ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB;
                    }
                    if (debug)
                    {
                        SendMessageToPC(OBJECT_SELF, BackgroundLoader.loaderError);
                    }

                    command = Users.GetUser(OBJECT_SELF).openCommand;
                }
            }

            string commandParam = (string)ScriptParameters[1];
            User currentUser = Users.GetUser(OBJECT_SELF);
            switch (command)
            {
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB:
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB:
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB:
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB:
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB:
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB:
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB:
                    CreatorTabs.FocusTabs(this, currentUser, command);
                    break;
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_SPAWN_LOOT:
                    DisplayGuiScreen(OBJECT_SELF, "SCREEN_ACR_LOOTGEN", FALSE, "acr_lootgeneration.xml", FALSE);
                    break;
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CREATOR_INCOMING_CLICK:
                    // TODO: make note of the selected row and provide
                    // additional information, if appropriate.
                    break;
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CREATOR_INCOMING_DOUBLECLICK:
                    if (commandParam.Contains(":"))
                    {
                        // first, default this stuff in. On error, we flip out and just use
                        // the bottom category in the creature navigator.
                        NavigatorCategory currentCat = Navigators.CreatureNavigator.bottomCategory;
                        NavigatorCategory targetCat = Navigators.CreatureNavigator.bottomCategory;

                        // then, we need to know where we are right now.
                        if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB) currentCat = currentUser.CurrentCreatureCategory;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB) currentCat = currentUser.CurrentItemCategory;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB) currentCat = currentUser.CurrentPlaceableCategory;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB) currentCat = currentUser.CurrentWaypointCategory;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB) currentCat = currentUser.CurrentVisualEffectCategory;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB) currentCat = currentUser.CurrentLightCategory;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB) currentCat = currentUser.CurrentTrapCategory;

                        // and we figure out where we're going relative to where we are.
                        string searchTerm = commandParam.Split(':')[1];
                        if (searchTerm == "..")
                        {
                            if (currentCat.ParentCategory != null)
                            {
                                targetCat = currentCat.ParentCategory;
                            }
                            else
                            {
                                if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB) targetCat = Navigators.CreatureNavigator.bottomCategory;
                                else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB) targetCat = Navigators.ItemNavigator.bottomCategory;
                                else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB) targetCat = Navigators.PlaceableNavigator.bottomCategory;
                                else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB) targetCat = Navigators.WaypointNavigator.bottomCategory;
                                else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB) targetCat = Navigators.VisualEffectNavigator.bottomCategory;
                                else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB) targetCat = Navigators.LightNavigator.bottomCategory;
                                else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB) targetCat = Navigators.TrapNavigator.bottomCategory;
                            }
                        }
                        else
                        {
                            if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentCreatureCategory, searchTerm);
                            else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentItemCategory, searchTerm);
                            else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentPlaceableCategory, searchTerm);
                            else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentWaypointCategory, searchTerm);
                            else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentVisualEffectCategory, searchTerm);
                            else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentLightCategory, searchTerm);
                            else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB) targetCat = BackgroundLoader.GetCategoryByName(currentUser.CurrentTrapCategory, searchTerm);
                        }

                        // and then we have a new current category.
                        if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB) currentUser.CurrentCreatureCategory = targetCat;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB) currentUser.CurrentItemCategory = targetCat;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB) currentUser.CurrentPlaceableCategory = targetCat;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB) currentUser.CurrentWaypointCategory = targetCat;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB) currentUser.CurrentVisualEffectCategory = targetCat;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB) currentUser.CurrentLightCategory = targetCat;
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB) currentUser.CurrentTrapCategory = targetCat;

                        // and finally we can draw the new navigator category.
                        Waiter.DrawNavigatorCategory(this, targetCat);
                    }
                    else
                    {
                        SendMessageToPC(OBJECT_SELF, "Preparing to spawn " + commandParam);

                        string spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_SINGLE;
                        string spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_SINGLE;

                        // The name of the script to execute on targeting.
                        SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_TARGET_SCRIPT_NAME, "gui_creatorspawn");

                        // The first string parameter being used.
                        SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_TARGET_SCRIPT_NAME_PARAM, commandParam);

                        if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, CLRScriptBase.OBJECT_TYPE_CREATURE.ToString());
                        }
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "self,creature,ground,placeable");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, CLRScriptBase.OBJECT_TYPE_ITEM.ToString());
                        }
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, CLRScriptBase.OBJECT_TYPE_PLACEABLE.ToString());
                        }
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, CLRScriptBase.OBJECT_TYPE_WAYPOINT.ToString());
                        }
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, CLRScriptBase.OBJECT_TYPE_PLACED_EFFECT.ToString());
                        }
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, CLRScriptBase.OBJECT_TYPE_LIGHT.ToString());
                        }
                        else if (currentUser.openCommand == ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB)
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, "99");
                            ALFA.Shared.TrapResource trapToSpawn = ALFA.Shared.Modules.InfoStore.ModuleTraps[commandParam];
                            if (trapToSpawn != null)
                            {
                                switch(trapToSpawn.TriggerArea)
                                    {
                                        case 2:
                                            spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_10FT;
                                            spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_10FT;
                                            break;
                                        case 3:
                                            spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_20FT;
                                            spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_20FT;
                                            break;
                                        case 4:
                                            spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_20FT;
                                            spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_20FT;
                                            break;
                                        case 5:
                                            spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_30FT;
                                            spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_30FT;
                                            break;
                                        case 6:
                                            spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_30FT;
                                            spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_30FT;
                                            break;
                                        default:
                                            spawnUI = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_SINGLE;
                                            spawnUIScreenName = ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_SINGLE;
                                            break;
                                    }
                            }
                        }
                        
                        DisplayGuiScreen(OBJECT_SELF, spawnUIScreenName, 0, spawnUI, 0);
                    }
                    // TODO: make note of the selected row and provide a suitable
                    // interface to direct the action.
                    break;
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_INCOMING_CLICK:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            int objType = GetObjectType(targetObject);
                            if (objType == OBJECT_TYPE_CREATURE)
                            {
                                if (GetIsPC(targetObject) == FALSE)
                                {
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "npc_creature", FALSE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "pc_creature", TRUE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "placedoor", TRUE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "spawn", TRUE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "other", TRUE);
                                }
                                else
                                {
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "npc_creature", TRUE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "pc_creature", FALSE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "placedoor", TRUE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "spawn", TRUE);
                                    SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "other", TRUE);
                                }
                            }
                            else if (objType == OBJECT_TYPE_DOOR ||
                                     objType == OBJECT_TYPE_PLACEABLE)
                            {
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "npc_creature", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "pc_creature", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "placedoor", FALSE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "spawn", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "other", TRUE);
                            }
                            else if (objType == OBJECT_TYPE_WAYPOINT &&
                                (!String.IsNullOrWhiteSpace(GetLocalString(targetObject, "ACR_SPAWN_GROUP_1")) ||
                                !String.IsNullOrWhiteSpace(GetLocalString(targetObject, "ACR_SPAWN_RESNAME_1")) ||
                                !String.IsNullOrWhiteSpace(GetLocalString(targetObject, "ACR_SPAWN_RANDOM_RESNAME_1"))))
                            {
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "npc_creature", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "pc_creature", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "placedoor", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "spawn", FALSE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "other", TRUE);
                            }
                            else
                            {
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "npc_creature", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "pc_creature", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "placedoor", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "spawn", TRUE);
                                SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "other", FALSE);
                            }
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_SEARCH_CREATOR:
                    {
                        if (commandParam == currentUser.LastSearchString &&
                            currentUser.openCommand == currentUser.LastSearchCommand)
                        {
                            if (currentUser.CreatorSearchResponse != null)
                            {
                                // They already have this one. We should just
                                // draw it.
                                Waiter.DrawNavigatorCategory(this, currentUser.CreatorSearchResponse);
                                return 0;
                            }
                        }
                        if (currentUser.CurrentSearch != null)
                        {
                            // In this case, a search is already running. We'll want to
                            // cancel it and clear it out.
                            CreatorSearch oldSearch = currentUser.CurrentSearch;
                            oldSearch.CancelAsync();
                            DelayCommand(0.5f, delegate { oldSearch.Dispose(); });
                            currentUser.CurrentSearch = null;
                        }
                        if (String.IsNullOrWhiteSpace(commandParam))
                        {
                            // If the search string is empty, we'll assume that they just
                            // want their old tab back.
                            CreatorTabs.FocusTabs(this, currentUser, currentUser.openCommand);
                            return 0;
                        }
                        
                        // From here, it looks like we have to do a real search.
                        currentUser.CurrentSearch = new CreatorSearch();
                        currentUser.CurrentSearch.WorkerSupportsCancellation = true;
                        currentUser.CurrentSearch.currentUser = currentUser;
                        currentUser.LastSearchString = commandParam.ToLower();
                        currentUser.LastSearchCommand = currentUser.openCommand;
                        currentUser.CreatorSearchResponse = null;
                        switch (currentUser.openCommand)
                        {
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentCreatureCategory;
                                break;
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentItemCategory;
                                break;
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentLightCategory;
                                break;
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentPlaceableCategory;
                                break;
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentTrapCategory;
                                break;
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_VFX_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentVisualEffectCategory;
                                break;
                            case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB:
                                currentUser.CurrentSearch.baseCat = currentUser.CurrentWaypointCategory;
                                break;
                        }
                        currentUser.CurrentSearch.DoWork += new System.ComponentModel.DoWorkEventHandler(currentUser.CurrentSearch.SearchCreator);
                        currentUser.CurrentSearch.RunWorkerAsync();
                        CreatorSearch.WaitForSearch(this, currentUser, currentUser.openCommand, currentUser.CurrentSearch);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_INITIALIZE_CHOOSER:
                    {
                        ChooserLists.InitializeButtons(this, currentUser);
                        ChooserLists.DrawAreas(this, currentUser);
                        currentUser.FocusedArea = GetArea(currentUser.Id);
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_CHOOSER:
                    {
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "ChooserActive", FALSE);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "Chooser", FALSE);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "LimboActive", TRUE);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "Limbo", TRUE);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_FOCUS_LIMBO:
                    {
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "ChooserActive", TRUE);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "Chooser", TRUE);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "LimboActive", FALSE);
                        SetGUIObjectHidden(OBJECT_SELF, "SCREEN_DMC_CHOOSER", "Limbo", FALSE);
                        ChooserLists.DrawLimbo(this, currentUser);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_SEARCH_CHOOSER:
                    {
                        if (String.IsNullOrWhiteSpace(commandParam))
                        {
                            ChooserLists.DrawAreas(this, currentUser);
                        }
                        else
                        {
                            ChooserLists.SearchAreas(this, currentUser, commandParam);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_LIST_AREA:
                    {
                        uint targetArea = 0;
                        if (uint.TryParse(commandParam, out targetArea))
                        {
                            currentUser.FocusedArea = targetArea;
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_AOE_VISIBLE:
                    {
                        currentUser.ChooserShowAOE = !currentUser.ChooserShowAOE;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_AOE", currentUser.ChooserShowAOE ? "trap.tga" : "notrap.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_CREATURE_VISIBLE:
                    {
                        currentUser.ChooserShowCreature = !currentUser.ChooserShowCreature;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_CREATURE", currentUser.ChooserShowCreature ? "creature.tga" : "nocreature.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_DOOR_VISIBLE:
                    {
                        currentUser.ChooserShowDoor = !currentUser.ChooserShowDoor;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_DOOR", currentUser.ChooserShowDoor ? "door.tga" : "nodoor.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_ITEM_VISIBLE:
                    {
                        currentUser.ChooserShowItem = !currentUser.ChooserShowItem;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_ITEM", currentUser.ChooserShowItem ? "item.tga" : "noitem.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_LIGHT_VISIBLE:
                    {
                        currentUser.ChooserShowLight = !currentUser.ChooserShowLight;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_LIGHT", currentUser.ChooserShowLight ? "light.tga" : "nolight.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_PLACEABLE_VISIBLE:
                    {
                        currentUser.ChooserShowPlaceable = !currentUser.ChooserShowPlaceable;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_PLACEABLE", currentUser.ChooserShowPlaceable ? "placeable.tga" : "noplaceable.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_PLACEDEFFECT_VISIBLE:
                    {
                        currentUser.ChooserShowPlacedEffect = !currentUser.ChooserShowPlacedEffect;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_VFX", currentUser.ChooserShowPlacedEffect ? "vfx.tga" : "novfx.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_STORE_VISIBLE:
                    {
                        currentUser.ChooserShowStore = !currentUser.ChooserShowStore;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_STORE", currentUser.ChooserShowStore ? "store.tga" : "nostore.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_TRIGGER_VISIBLE:
                    {
                        currentUser.ChooserShowTrigger = !currentUser.ChooserShowTrigger;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_TRIGGER", currentUser.ChooserShowTrigger ? "trigger.tga" : "notrigger.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_WAYPOINT_VISIBLE:
                    {
                        currentUser.ChooserShowWaypoint = !currentUser.ChooserShowWaypoint;
                        SetGUITexture(currentUser.Id, "SCREEN_DMC_CHOOSER", "SHOW_WAYPOINT", currentUser.ChooserShowWaypoint ? "waypoint.tga" : "nowaypoint.tga");
                        ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_JUMP_TO_AREA:
                    {
                        ChooserJump.JumpToArea(this, currentUser);
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_JUMP_TO_OBJECT:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            JumpToLocation(GetLocation(targetObject));
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_JUMP_OBJECT_TO_ME:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_TARGET_SCRIPT_NAME, "gui_chooserjump");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_TARGET_SCRIPT_NAME_PARAM, commandParam);
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, "0");
                            DisplayGuiScreen(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_SINGLE, 0, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_SINGLE, 0);
                            SendMessageToPC(currentUser.Id, String.Format("Jumping {0} to your target...", GetTag(targetObject)));
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_HEAL:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(1000), targetObject, 0.0f);
                            foreach (NWEffect eff in GetEffects(targetObject))
                            {
                                RemoveEffect(targetObject, eff);
                            }
                            SendMessageToPC(currentUser.Id, String.Format("Healing {0}...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_IMMORTAL:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            if (GetImmortal(targetObject) == TRUE)
                            {
                                SetImmortal(targetObject, FALSE);
                                SendMessageToPC(currentUser.Id, String.Format("Making {0} immortal...", GetTag(targetObject)));
                            }
                            else
                            {
                                SetImmortal(targetObject, TRUE);
                                SendMessageToPC(currentUser.Id, String.Format("Making {0} mortal...", GetTag(targetObject)));
                            }
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }                       
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_HOSTILE:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            ChangeToStandardFaction(targetObject, STANDARD_FACTION_HOSTILE);
                            SendMessageToPC(currentUser.Id, String.Format("Making {0} hostile...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_NONHOSTILE:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            ChangeToStandardFaction(targetObject, STANDARD_FACTION_COMMONER);
                            SendMessageToPC(currentUser.Id, String.Format("Making {0} non-hostile...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_KILL:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetImmortal(targetObject, FALSE);
                            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDeath(FALSE, FALSE, TRUE, TRUE), targetObject, 0.0f);
                            SendMessageToPC(currentUser.Id, String.Format("Killing {0}...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_LIMBO:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SendCreatureToLimbo(targetObject);
                            SendMessageToPC(currentUser.Id, String.Format("Sending {0} to limbo...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_RESTORE:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            foreach (NWEffect eff in GetEffects(targetObject))
                            {
                                int effType = GetEffectType(eff);
                                if (effType == EFFECT_TYPE_ABILITY_DECREASE ||
                                    effType == EFFECT_TYPE_AC_DECREASE ||
                                    effType == EFFECT_TYPE_ASSAYRESISTANCE ||
                                    effType == EFFECT_TYPE_ATTACK_DECREASE ||
                                    effType == EFFECT_TYPE_BLINDNESS ||
                                    effType == EFFECT_TYPE_CHARMED ||
                                    effType == EFFECT_TYPE_CONFUSED ||
                                    effType == EFFECT_TYPE_CURSE ||
                                    effType == EFFECT_TYPE_CUTSCENE_PARALYZE ||
                                    effType == EFFECT_TYPE_CUTSCENEIMMOBILIZE ||
                                    effType == EFFECT_TYPE_DAMAGE_DECREASE ||
                                    effType == EFFECT_TYPE_DAMAGE_IMMUNITY_DECREASE ||
                                    effType == EFFECT_TYPE_DAZED ||
                                    effType == EFFECT_TYPE_DEAF ||
                                    effType == EFFECT_TYPE_DISEASE ||
                                    effType == EFFECT_TYPE_DOMINATED ||
                                    effType == EFFECT_TYPE_ENTANGLE ||
                                    effType == EFFECT_TYPE_FRIGHTENED ||
                                    effType == EFFECT_TYPE_INSANE ||
                                    effType == EFFECT_TYPE_MESMERIZE ||
                                    effType == EFFECT_TYPE_MOVEMENT_SPEED_DECREASE ||
                                    effType == EFFECT_TYPE_NEGATIVELEVEL ||
                                    effType == EFFECT_TYPE_PARALYZE ||
                                    effType == EFFECT_TYPE_PETRIFY ||
                                    effType == EFFECT_TYPE_POISON ||
                                    effType == EFFECT_TYPE_SAVING_THROW_DECREASE ||
                                    effType == EFFECT_TYPE_SKILL_DECREASE ||
                                    effType == EFFECT_TYPE_SLOW ||
                                    effType == EFFECT_TYPE_SPELL_RESISTANCE_DECREASE ||
                                    effType == EFFECT_TYPE_STUNNED ||
                                    effType == EFFECT_TYPE_TURN_RESISTANCE_DECREASE ||
                                    effType == EFFECT_TYPE_TURNED ||
                                    effType == EFFECT_TYPE_WOUNDING)
                                {
                                    RemoveEffect(targetObject, eff);
                                }
                            }
                            SendMessageToPC(currentUser.Id, String.Format("Removing abnormalities from {0}...", GetName(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_REST:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SendMessageToPC(OBJECT_SELF, "Resetting rest for " + GetName(targetObject) + ".");
                            GetDatabase().ACR_DeletePersistentVariable(targetObject, ACR_REST_TIMER);
                            SendMessageToPC(currentUser.Id, String.Format("Allowing {0} to rest...", GetName(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_SPELLPREP:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SendMessageToPC(OBJECT_SELF, "Resetting spell timers for " + GetName(targetObject) + ".");
                            GetDatabase().ACR_DeletePersistentVariable(targetObject, ACR_REST_STUDY_TIMER);
                            GetDatabase().ACR_DeletePersistentVariable(targetObject, ACR_REST_PRAYER_TIMER);
                            SendMessageToPC(currentUser.Id, String.Format("Allowing {0} to prepare spells...", GetName(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_VALIDATE:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            if (GetIsPC(targetObject) == FALSE)
                            {
                                SendMessageToPC(OBJECT_SELF, "That is not a PC, and thus cannot be validated from quarantine.");
                                return 0;
                            }
                            if (GetDatabase().ACR_GetIsPCQuarantined(targetObject))
                            {
                                // False alarm. Notify the DM, end.
                                SendMessageToPC(OBJECT_SELF, GetName(targetObject) + " is not flagged as Quarantined.");
                                return 0;
                            }
                            // store the current server and location as valid
                            GetDatabase().ACR_PCUpdateStatus(targetObject, true);
                            // clear the local quarantine flag on the PC
                            DeleteLocalInt(targetObject, ACR_PPS_QUARANTINED);
                            // re-run the rest initialization
                            GetDatabase().ACR_RestOnClientEnter(targetObject);
                            // Validate the targeted PC.
                            GetDatabase().ACR_PPSValidatePC(targetObject);
                            // start the XP system as well
                            GetDatabase().ACR_XPOnClientLoaded(targetObject);
                            GetDatabase().ACR_LogEvent(targetObject, ACR_LOG_VALIDATED, "Validated for play on server " + GetName(GetModule()) + " by DM: " + GetName(OBJECT_SELF), OBJECT_SELF);
                            GetDatabase().ACR_SetPersistentString(targetObject, _ACR_PTL_RECORD, "Validated for serverID " + IntToString(GetDatabase().ACR_GetServerID()) + " by DM: " + GetName(OBJECT_SELF));
                            GetDatabase().ACR_DeletePersistentVariable(targetObject, _ACR_PTL_PASSPORT);
                            SendMessageToPC(targetObject, "Validated and normalized by DM: " + GetName(OBJECT_SELF) + ".");
                            SendMessageToAllDMs("PC: " + GetName(targetObject) + " was validated from quarantine by DM: " + GetName(OBJECT_SELF));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_VIEW_INVENTORY:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            ClearScriptParams();
                            AddScriptParameterInt(PLAYER_REPORT_SHOW_INVENTORY);
                            AddScriptParameterObject(targetObject);
                            ExecuteScriptEnhanced("gui_playerreport", OBJECT_SELF, TRUE);
                            SendMessageToPC(currentUser.Id, String.Format("Opening {0}'s inventory...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_LOCK:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetLocked(targetObject, TRUE);
                            SendMessageToPC(currentUser.Id, String.Format("Locking {0}...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_UNLOCK:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetLocked(targetObject, FALSE);
                            SendMessageToPC(currentUser.Id, String.Format("Unlocking {0}...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_PLOT:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetPlotFlag(targetObject, TRUE);
                            SendMessageToPC(currentUser.Id, String.Format("Setting {0} as plot...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_UNPLOT:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetPlotFlag(targetObject, FALSE);
                            SendMessageToPC(currentUser.Id, String.Format("Setting {0} as non-plot...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_DESTROY:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            int objType = GetObjectType(targetObject);
                            if (objType == OBJECT_TYPE_AREA_OF_EFFECT &&
                                GetTag(targetObject).ToLower().Contains("trap"))
                            {
                                ClearScriptParams();
                                AddScriptParameterInt(TRAP_EVENT_DESPAWN_TRAP);
                                AddScriptParameterFloat(0.0f);
                                AddScriptParameterFloat(0.0f);
                                AddScriptParameterFloat(0.0f);
                                AddScriptParameterObject(OBJECT_INVALID);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterFloat(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterObject(OBJECT_INVALID);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
                                AddScriptParameterInt(-1);
	                            AddScriptParameterString("");
		                        ExecuteScriptEnhanced("acr_traps", targetObject, TRUE);
                                ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                                SendMessageToPC(currentUser.Id, String.Format("Disabling {0} as a trap...", GetTag(targetObject)));
                            }
                            else
                            {
                                DestroyObject(targetObject, 0.0f, FALSE);
                                ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                                if (GetObjectType(targetObject) == OBJECT_TYPE_CREATURE)
                                {
                                    // DestroyObject runs at the end of script execution, so we have to delay
                                    // to appropriately redraw the lists.
                                    DelayCommand(0.1f, delegate { ChooserLists.DrawLimbo(this, currentUser); });
                                }
                                SendMessageToPC(currentUser.Id, String.Format("Destroying {0}...", GetTag(targetObject)));
                            }
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_UNTRAP:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetTrapActive(targetObject, FALSE);
                            SendMessageToPC(currentUser.Id, String.Format("Removing trap from {0}...", GetTag(targetObject)));
                            ChooserLists.DrawObjects(this, currentUser, currentUser.FocusedArea);
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_UNLIMBO:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_TARGET_SCRIPT_NAME, "gui_chooserjump");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_TARGET_SCRIPT_NAME_PARAM, commandParam);
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_VALID_TARGET_LIST, "ground");
                            SetGlobalGUIVariable(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_CREATOR_CREATE_OBJECT_TYPE, "999");
                            DisplayGuiScreen(OBJECT_SELF, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_UINAME_SINGLE, 0, ALFA.Shared.GuiGlobals.ACR_GUI_GLOBAL_TARGETUI_SINGLE, 0);
                            SendMessageToPC(currentUser.Id, String.Format("Recalling {0} from limbo...", GetTag(targetObject)));
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_ACTIVATE_SPAWN:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetLocalInt(targetObject, "ACR_SPAWN_IS_DISABLED", 0);
                            SendMessageToPC(currentUser.Id, String.Format("Enabling spawns from {0}...", GetTag(targetObject)));
                        }
                        break;
                    }
                case ACR_CreatorCommand.ACR_CHOOSERCREATOR_CHOOSER_DEACTIVATE_SPAWN:
                    {
                        uint targetObject = 0;
                        if (uint.TryParse(commandParam, out targetObject))
                        {
                            SetLocalInt(targetObject, "ACR_SPAWN_IS_DISABLED", 1);
                            SendMessageToPC(currentUser.Id, String.Format("Disabling spawns from {0}...", GetTag(targetObject)));
                        }
                        break;
                    }
            }
            return 0;

        }

        public enum ACR_CreatorCommand
        {
            ACR_CHOOSERCREATOR_INITIALIZE_LISTS = 0,

            ACR_CHOOSERCREATOR_FOCUS_CREATURE_TAB = 1,
            ACR_CHOOSERCREATOR_FOCUS_ITEM_TAB = 2,
            ACR_CHOOSERCREATOR_FOCUS_PLACEABLE_TAB = 3,
            ACR_CHOOSERCREATOR_FOCUS_TRAP_TAB = 4,
            ACR_CHOOSERCREATOR_FOCUS_VFX_TAB = 5,
            ACR_CHOOSERCREATOR_FOCUS_WAYPOINT_TAB = 6,
            ACR_CHOOSERCREATOR_FOCUS_LIGHTS_TAB = 7,

            ACR_CHOOSERCREATOR_CREATOR_INCOMING_CLICK = 20,
            ACR_CHOOSERCREATOR_CREATOR_INCOMING_DOUBLECLICK = 21,
            ACR_CHOOSERCREATOR_CHOOSER_INCOMING_CLICK = 22,

            ACR_CHOOSERCREATOR_SEARCH_CREATOR = 31,

            ACR_CHOOSERCREATOR_SPAWN_LOOT = 41,

            ACR_CHOOSERCREATOR_INITIALIZE_CHOOSER = 100,
            ACR_CHOOSERCREATOR_FOCUS_CHOOSER = 101,
            ACR_CHOOSERCREATOR_FOCUS_LIMBO = 102,

            ACR_CHOOSERCREATOR_SEARCH_CHOOSER = 111,
            ACR_CHOOSERCREATOR_LIST_AREA = 112,

            ACR_CHOOSERCREATOR_CHOOSER_AOE_VISIBLE = 121,
            ACR_CHOOSERCREATOR_CHOOSER_CREATURE_VISIBLE = 122,
            ACR_CHOOSERCREATOR_CHOOSER_DOOR_VISIBLE = 123, 
            ACR_CHOOSERCREATOR_CHOOSER_ITEM_VISIBLE = 124,
            ACR_CHOOSERCREATOR_CHOOSER_LIGHT_VISIBLE = 125,
            ACR_CHOOSERCREATOR_CHOOSER_PLACEABLE_VISIBLE = 126,
            ACR_CHOOSERCREATOR_CHOOSER_PLACEDEFFECT_VISIBLE = 127,
            ACR_CHOOSERCREATOR_CHOOSER_STORE_VISIBLE = 128,
            ACR_CHOOSERCREATOR_CHOOSER_TRIGGER_VISIBLE = 129,
            ACR_CHOOSERCREATOR_CHOOSER_WAYPOINT_VISIBLE = 130,

            ACR_CHOOSERCREATOR_CHOOSER_JUMP_TO_AREA = 131,
            ACR_CHOOSERCREATOR_CHOOSER_JUMP_TO_OBJECT = 132,
            ACR_CHOOSERCREATOR_CHOOSER_JUMP_OBJECT_TO_ME = 133,
            ACR_CHOOSERCREATOR_CHOOSER_HEAL = 134,
            ACR_CHOOSERCREATOR_CHOOSER_IMMORTAL = 135,
            ACR_CHOOSERCREATOR_CHOOSER_HOSTILE = 136,
            ACR_CHOOSERCREATOR_CHOOSER_NONHOSTILE = 137,
            ACR_CHOOSERCREATOR_CHOOSER_KILL = 138,
            ACR_CHOOSERCREATOR_CHOOSER_LIMBO = 139,
            ACR_CHOOSERCREATOR_CHOOSER_RESTORE = 140,
            ACR_CHOOSERCREATOR_CHOOSER_REST = 141,
            ACR_CHOOSERCREATOR_CHOOSER_SPELLPREP = 142,
            ACR_CHOOSERCREATOR_CHOOSER_VALIDATE = 143,
            ACR_CHOOSERCREATOR_CHOOSER_VIEW_INVENTORY = 144,
            ACR_CHOOSERCREATOR_CHOOSER_LOCK = 145,
            ACR_CHOOSERCREATOR_CHOOSER_UNLOCK = 146,
            ACR_CHOOSERCREATOR_CHOOSER_PLOT = 147,
            ACR_CHOOSERCREATOR_CHOOSER_UNPLOT = 148,
            ACR_CHOOSERCREATOR_CHOOSER_DESTROY = 149,
            ACR_CHOOSERCREATOR_CHOOSER_UNTRAP = 150,
            ACR_CHOOSERCREATOR_CHOOSER_UNLIMBO = 151,
            ACR_CHOOSERCREATOR_CHOOSER_ACTIVATE_SPAWN = 152,
            ACR_CHOOSERCREATOR_CHOOSER_DEACTIVATE_SPAWN = 153,
        }

    }
}
