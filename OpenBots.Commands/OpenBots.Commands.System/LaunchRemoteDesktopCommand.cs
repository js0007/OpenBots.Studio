﻿using OpenBots.Commands.System.Forms;
using OpenBots.Core.Attributes.PropertyAttributes;
using OpenBots.Core.Command;
using OpenBots.Core.Enums;
using OpenBots.Core.Infrastructure;
using OpenBots.Core.Properties;
using OpenBots.Core.UI.Controls;
using OpenBots.Core.Utilities.CommonUtilities;
using OpenBots.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Windows.Forms;

namespace OpenBots.Commands.System
{
	[Serializable]
	[Category("System Commands")]
	[Description("This command launches a remote desktop session.")]
	public class LaunchRemoteDesktopCommand : ScriptCommand
	{
		[Required]
		[DisplayName("Machine Name")]
		[Description("Define the name of the machine to log on to.")]
		[SampleUsage("myMachine || {vMachineName}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_MachineName { get; set; }

		[Required]
		[DisplayName("Username")]
		[Description("Define the username to use when connecting to the machine.")]
		[SampleUsage("myRobot || {vUsername}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_UserName { get; set; }

		[Required]
		[DisplayName("Password")]
		[Description("Define the password to use when connecting to the machine.")]
		[SampleUsage("password || {vPassword}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_Password { get; set; }

		[Required]
		[DisplayName("RDP Window Width")]
		[Description("Define the width for the Remote Desktop Window.")]
		[SampleUsage("1000 || {vWidth}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_RDPWidth { get; set; }

		[Required]
		[DisplayName("RDP Window Height")]
		[Description("Define the height for the Remote Desktop Window.")]
		[SampleUsage("800 || {vHeight}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_RDPHeight { get; set; }

		public LaunchRemoteDesktopCommand()
		{
			CommandName = "LaunchRemoteDesktopCommand";
			SelectionName = "Launch Remote Desktop";
			CommandEnabled = true;        

			v_RDPWidth = SystemInformation.PrimaryMonitorSize.Width.ToString();
			v_RDPHeight = SystemInformation.PrimaryMonitorSize.Height.ToString();
		}

		public override void RunCommand(object sender)
		{
			var engine = (AutomationEngineInstance)sender;
			var machineName = v_MachineName.ConvertUserVariableToString(engine);
			var userName = v_UserName.ConvertUserVariableToString(engine);
			var password = v_Password.ConvertUserVariableToString(engine);
			var width = int.Parse(v_RDPWidth.ConvertUserVariableToString(engine));
			var height = int.Parse(v_RDPHeight.ConvertUserVariableToString(engine));

			if (engine.ScriptEngineUI != null)
			{
				var result = ((Form)engine.ScriptEngineUI).Invoke(new Action(() =>
				{
					LaunchRDPSession(machineName, userName, password, width, height);
				}));
			}
			else
				LaunchRDPSession(machineName, userName, password, width, height);
		}

		public override List<Control> Render(IfrmCommandEditor editor, ICommandControls commandControls)
		{
			base.Render(editor, commandControls);

			CommandItemControl helperControl = new CommandItemControl();

			helperControl.Padding = new Padding(10, 0, 0, 0);
			helperControl.ForeColor = Color.AliceBlue;
			helperControl.Font = new Font("Segoe UI Semilight", 10);
			helperControl.CommandImage = Resources.command_system;
			helperControl.CommandDisplay = "RDP Display Manager";
			helperControl.Click += new EventHandler((s, e) => LaunchRDPDisplayManager(s, e));

			RenderedControls.Add(helperControl);

			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_MachineName", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_UserName", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultPasswordInputGroupFor("v_Password", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_RDPWidth", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_RDPHeight", this, editor));

			return RenderedControls;
		}

		public override string GetDisplayValue()
		{
			return base.GetDisplayValue() + $" [Machine '{v_MachineName}']";
		}

		public void LaunchRDPDisplayManager(object sender, EventArgs e)
		{
			frmDisplayManager displayManager = new frmDisplayManager();
			displayManager.ShowDialog();
			displayManager.Close();            
		}

		public void LaunchRDPSession(string machineName, string userName, string password, int width, int height)
		{
			var remoteDesktopForm = new frmRemoteDesktopViewer(machineName, userName, password, width, height, false, false);
			remoteDesktopForm.Show();
		}
	}
}
