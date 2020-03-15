using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        //Group used by this script. It should include all the projectors that are part of the
        // same managed group and also lcd screens to display information (info, desc)
        string ProjectorGroup = "[sb] Projectors";
        
        //Optional

        // Projector prefix that will be ignored when displaying name of the ship
        string ProjectorsPrefix = "pj - ";

        // Info / description keyword. Lcds should be member of the projector group
        string InfoLCDFilter = "info"; // lcd screen that will show Name / Total block / Remaining blocks
        string DescriptionLCDFilter = "desc"; //lcd screen that will show projector custom data, if defined.
        string ListLCDFilter = "list"; // Not used yet

        // If set to True, projectors will show only buildable block provided at least 1 block can be welded.
        bool ShowBuildableOnly = false;


        // DO NOT MODIFY ANYTHING BELOW THIS LINE

        static int CycleTicks = 500;
        //string UnassignedProjectorFilter = "Unassigned";

        IMyProjector _CurrentProjector = null;
        int _CurrentProjectorRemainingBlocks = 0;


        List<IMyProjector> _Projectors = new List<IMyProjector>();
        List<IMyTextPanel> _LCDPanels = new List<IMyTextPanel>();
        IMyTextPanel _InfoLCD = null;
        IMyTextPanel _DescLCD = null;
        IMyTextPanel _ListLCD = null;
        int CycleTicksCurrent = CycleTicks;
        bool CycleEnabled = false;
        int _CycleCurrentIndex = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource.HasFlag(UpdateType.Once))
            {
                Init();
                Runtime.UpdateFrequency = UpdateFrequency.Update10;

            }
            CycleShips();
            if (updateSource.HasFlag(UpdateType.Terminal))
            {
                if (argument == "cycle")
                {
                    CycleEnabled = !CycleEnabled;
                }

                

             
            }

            if (updateSource.HasFlag(UpdateType.Update10)) {
                bool pjChanged = ProjectorChanged();
                bool lcdChanged = pjChanged || _CurrentProjector?.RemainingBlocks != _CurrentProjectorRemainingBlocks;

                if (lcdChanged)
                {
                    UpdateLCD();
                }
            }

        
        }

        public void Init()
        {
            IMyBlockGroup blocks = GridTerminalSystem.GetBlockGroupWithName(ProjectorGroup);
            blocks.GetBlocksOfType<IMyProjector>(_Projectors);
            blocks.GetBlocksOfType<IMyTextPanel>(_LCDPanels);
            _InfoLCD = null;
            _ListLCD = null;
            _DescLCD = null;
            foreach (IMyTextPanel lcd in _LCDPanels)
            {
                if (lcd.ContentType != ContentType.TEXT_AND_IMAGE) { lcd.ContentType = ContentType.TEXT_AND_IMAGE; }
                if (lcd.DisplayNameText.Contains(InfoLCDFilter)) { _InfoLCD = lcd; }
                else if (lcd.DisplayNameText.Contains(DescriptionLCDFilter)) { _DescLCD = lcd; }
                else if (lcd.DisplayNameText.Contains(ListLCDFilter)) { _ListLCD = lcd; }
            }

        }

        public bool ProjectorChanged()
        {
            bool ProjectorChanged = false;
            int EnabledCount = 0;
            foreach (IMyProjector pj in _Projectors)
            {
                if (pj.Enabled)
                {
                    EnabledCount += 1;
                    if (_CurrentProjector == null)
                    {
                        _CurrentProjector = pj;
                        ProjectorChanged = true;
                    }
                    else if (_CurrentProjector.DisplayNameText.Equals(pj.DisplayNameText) == false)
                    {
                        _CurrentProjector.Enabled = false;
                        _CurrentProjector = pj;
                        ProjectorChanged = true;
                    }
                }

            }

            if (ProjectorChanged && _CurrentProjector != null)
            {
                bool DoShoWBuildableOnly = ShowBuildableOnly == true && _CurrentProjector.BuildableBlocksCount > 0;
                if (_CurrentProjector.ShowOnlyBuildable != DoShoWBuildableOnly)
                {
                    _CurrentProjector.ShowOnlyBuildable = DoShoWBuildableOnly;
                    _CurrentProjector.UpdateOffsetAndRotation();
                }
            }

            if (EnabledCount == 0 && _CurrentProjector != null)
            {
                _CurrentProjector = null;
                ProjectorChanged = true;
            }
            return ProjectorChanged;
        }

        public void CycleShips()
        {
            if (CycleEnabled)
            {
                if (CycleTicksCurrent > 0)
                {
                    CycleTicksCurrent -= 10;
                    Echo(CycleTicksCurrent.ToString());
                    Echo(_CycleCurrentIndex.ToString());
                    Echo(_CurrentProjector.DisplayNameText);
                }
                else
                {
                    CycleTicksCurrent = CycleTicks;
                    if (_CurrentProjector != null)
                    {
                        _CurrentProjector.Enabled = false;
                    }
                    _CurrentProjector = _Projectors[_CycleCurrentIndex];
                    _CurrentProjector.Enabled = true;
                    _CycleCurrentIndex += 1;
                    if (_CycleCurrentIndex > _Projectors.Count - 1)
                    {
                        _CycleCurrentIndex = 0;
                    }
                    
                    UpdateLCD();
                }
            }
        }

        public void UpdateLCD()
        {
            if (_CurrentProjector == null)
            {
                if (_InfoLCD != null) { _InfoLCD.WriteText(""); }
                if (_DescLCD != null) { _DescLCD.WriteText(""); }
                if (_ListLCD != null) { _ListLCD.WriteText(""); }
                return;
            }

            if (_InfoLCD != null)
            {
                StringBuilder OutputInfo = new StringBuilder();
                OutputInfo.AppendLine(_CurrentProjector.DisplayNameText.Replace(ProjectorsPrefix, ""));
                OutputInfo.AppendFormat("{0} blocks \n", _CurrentProjector.TotalBlocks.ToString());
                OutputInfo.AppendFormat("{0} remaining\n", _CurrentProjector.RemainingBlocks.ToString());
                _InfoLCD.WriteText(OutputInfo.ToString());
            }

            if (_DescLCD != null && _CurrentProjector?.CustomData != null)
            {
                _DescLCD.WriteText(_CurrentProjector.CustomData);
            }

            if (_ListLCD != null)
            {

            }

        }

    }
}
