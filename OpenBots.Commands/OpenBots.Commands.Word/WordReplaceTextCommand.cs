﻿using Microsoft.Office.Interop.Word;
using OpenBots.Core.Attributes.PropertyAttributes;
using OpenBots.Core.Command;
using OpenBots.Core.Enums;
using OpenBots.Core.Infrastructure;
using OpenBots.Core.Utilities.CommonUtilities;
using OpenBots.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;
using Application = Microsoft.Office.Interop.Word.Application;

namespace OpenBots.Commands.Word
{
	[Serializable]
	[Category("Word Commands")]
	[Description("This command replaces specific text in a Word Document.")]
	public class WordReplaceTextCommand : ScriptCommand
	{
		[Required]
		[DisplayName("Word Instance Name")]
		[Description("Enter the unique instance that was specified in the **Create Application** command.")]
		[SampleUsage("MyWordInstance")]
		[Remarks("Failure to enter the correct instance or failure to first call the **Create Application** command will cause an error.")]
		public string v_InstanceName { get; set; }

		[Required]
		[DisplayName("Find")]
		[Description("Enter the text to find.")]
		[SampleUsage("old text || {vFindText}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_FindText { get; set; }

		[Required]
		[DisplayName("Replace")]
		[Description("Enter the text to replace with.")]
		[SampleUsage("new text || {vReplaceText}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_ReplaceWithText { get; set; }

		public WordReplaceTextCommand()
		{
			CommandName = "WordReplaceTextCommand";
			SelectionName = "Replace Text";
			CommandEnabled = true;
			v_InstanceName = "DefaultWord";
		}
		public override void RunCommand(object sender)
		{
			var engine = (AutomationEngineInstance)sender;
			var vFindText = v_FindText.ConvertUserVariableToString(engine);
			var vReplaceWithText = v_ReplaceWithText.ConvertUserVariableToString(engine);

			//get word app object
			var wordObject = v_InstanceName.GetAppInstance(engine);

			//convert object
			Application wordInstance = (Application)wordObject;
			Document wordDocument = wordInstance.ActiveDocument;
			Range range = wordDocument.Content;

			//replace text
			Find findObject = range.Find;
			findObject.ClearFormatting();
			findObject.Text = vFindText;
			findObject.Replacement.ClearFormatting();
			findObject.Replacement.Text = vReplaceWithText;

			object replaceAll = WdReplace.wdReplaceAll;
			findObject.Execute(Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
							   Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
							   ref replaceAll, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

		}

		public override List<Control> Render(IfrmCommandEditor editor, ICommandControls commandControls)
		{
			base.Render(editor, commandControls);

			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_InstanceName", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_FindText", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_ReplaceWithText", this, editor));

			return RenderedControls;
		}

		public override string GetDisplayValue()
		{
			return base.GetDisplayValue() + $" [Replace '{v_FindText}' With '{v_ReplaceWithText}' - Instance Name '{v_InstanceName}']";
		}
	}
}