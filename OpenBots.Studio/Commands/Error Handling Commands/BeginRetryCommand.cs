﻿using Newtonsoft.Json;
using OpenBots.Core.Attributes.PropertyAttributes;
using OpenBots.Core.Command;
using OpenBots.Core.Enums;
using OpenBots.Core.Infrastructure;
using OpenBots.Core.Script;
using OpenBots.Core.UI.Controls;
using OpenBots.Core.Utilities.CommonUtilities;
using OpenBots.Engine;
using OpenBots.UI.Forms;
using OpenBots.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace OpenBots.Commands
{
	[Serializable]
	[Category("Error Handling Commands")]
	[Description("This command defines a retry block which will retry the contained commands as long as the condition is not met or " +
		"an error is thrown.")]
	public class BeginRetryCommand : ScriptCommand
	{
		[Required]
		[DisplayName("Number of Retries")]
		[Description("Enter or provide the number of retries.")]
		[SampleUsage("3 || {vRetryCount}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_RetryCount { get; set; }

		[Required]
		[DisplayName("Retry Interval")]
		[Description("Enter or provide the amount of time (in seconds) between each retry.")]
		[SampleUsage("5 || {vRetryInterval}")]
		[Remarks("")]
		[Editor("ShowVariableHelper", typeof(UIAdditionalHelperType))]
		public string v_RetryInterval { get; set; }

		[Required]
		[DisplayName("Condition")]
		[Description("Add a condition.")]
		[SampleUsage("")]
		[Remarks("Items in the retry scope will be executed if the condition doesn't satisfy.")]
		[Editor("ShowIfBuilder", typeof(UIAdditionalHelperType))]
		public DataTable v_IfConditionsTable { get; set; }

		[JsonIgnore]
		[Browsable(false)]
		private DataGridView _ifConditionHelper;

		[JsonIgnore]
		[Browsable(false)]
		private List<ScriptVariable> _scriptVariables { get; set; }

		[JsonIgnore]
		[Browsable(false)]
		private List<ScriptElement> _scriptElements { get; set; }

		public BeginRetryCommand()
		{
			CommandName = "BeginRetryCommand";
			SelectionName = "Begin Retry";
			CommandEnabled = true;          

			v_IfConditionsTable = new DataTable();
			v_IfConditionsTable.TableName = DateTime.Now.ToString("MultiIfConditionTable" + DateTime.Now.ToString("MMddyy.hhmmss"));
			v_IfConditionsTable.Columns.Add("Statement");
			v_IfConditionsTable.Columns.Add("CommandData");
		}

		public override void RunCommand(object sender, ScriptAction parentCommand)
		{
			//get engine
			var engine = (AutomationEngineInstance)sender;
			var retryCommand = (BeginRetryCommand)parentCommand.ScriptCommand;

			int retryCount = int.Parse(retryCommand.v_RetryCount.ConvertUserVariableToString(engine));
			int retryInterval = int.Parse(retryCommand.v_RetryInterval.ConvertUserVariableToString(engine))*1000;
			bool exceptionOccurred;

			for(int startIndex = 0; startIndex < retryCount; startIndex++)
			{
				exceptionOccurred = false;
				foreach(var cmd in parentCommand.AdditionalScriptCommands)
				{
					try
					{
						cmd.IsExceptionIgnored = true;
						engine.ExecuteCommand(cmd);
					}
					catch (Exception)
					{
						exceptionOccurred = true;
						break;
					}
				}
				// If no exception is thrown out and the Condition's satisfied
				if (!exceptionOccurred && GetConditionResult(sender))
				{
					retryCount = 0;
					engine.ErrorsOccured.Clear();
				}
				else if((startIndex+1) != retryCount)
				{
					Thread.Sleep(retryInterval);
				}
			}
		}

		public override List<Control> Render(IfrmCommandEditor editor, ICommandControls commandControls)
		{
			base.Render(editor, commandControls);

			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_RetryCount", this, editor));
			RenderedControls.AddRange(commandControls.CreateDefaultInputGroupFor("v_RetryInterval", this, editor));

			//get script variables for feeding into if builder form
			_scriptVariables = editor.ScriptVariables;
			_scriptElements = editor.ScriptElements;

			//create controls
			var controls = commandControls.CreateDataGridViewGroupFor("v_IfConditionsTable", this, editor);
			_ifConditionHelper = controls[2] as DataGridView;

			//handle helper click
			var helper = controls[1] as CommandItemControl;
			helper.Click += (sender, e) => CreateIfCondition(sender, e);

			//add for rendering
			RenderedControls.AddRange(controls);

			//define if condition helper
			_ifConditionHelper.Width = 450;
			_ifConditionHelper.Height = 200;
			_ifConditionHelper.AutoGenerateColumns = false;
			_ifConditionHelper.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
			_ifConditionHelper.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Condition", DataPropertyName = "Statement", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
			_ifConditionHelper.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "CommandData", DataPropertyName = "CommandData", ReadOnly = true, Visible = false });
			_ifConditionHelper.Columns.Add(new DataGridViewButtonColumn() { HeaderText = "Edit", UseColumnTextForButtonValue = true, Text = "Edit", Width = 45 });
			_ifConditionHelper.Columns.Add(new DataGridViewButtonColumn() { HeaderText = "Delete", UseColumnTextForButtonValue = true, Text = "Delete", Width = 60 });
			_ifConditionHelper.AllowUserToAddRows = false;
			_ifConditionHelper.AllowUserToDeleteRows = true;
			_ifConditionHelper.CellContentClick += IfConditionHelper_CellContentClick;

			return RenderedControls;
		}

		public override string GetDisplayValue()
		{
			return base.GetDisplayValue() + $" [Number of Retries '{v_RetryCount}' - Retry Interval '{v_RetryInterval}']";
		}

		private void IfConditionHelper_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			var senderGrid = (DataGridView)sender;

			if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
			{
				var buttonSelected = senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
				var selectedRow = v_IfConditionsTable.Rows[e.RowIndex];

				if (buttonSelected.Value.ToString() == "Edit")
				{
					//launch editor
					var statement = selectedRow["Statement"];
					var commandData = selectedRow["CommandData"].ToString();

					var ifCommand = JsonConvert.DeserializeObject<BeginIfCommand>(commandData);

					var automationCommands = UIControlsHelper.GenerateCommandsandControls().Where(f => f.Command is BeginIfCommand).ToList();
					frmCommandEditor editor = new frmCommandEditor(automationCommands, null);
					editor.SelectedCommand = ifCommand;
					editor.EditingCommand = ifCommand;
					editor.OriginalCommand = ifCommand;
					editor.CreationModeInstance = CreationMode.Edit;
					editor.ScriptVariables = _scriptVariables;
					editor.ScriptElements = _scriptElements;

					if (editor.ShowDialog() == DialogResult.OK)
					{
						var editedCommand = editor.EditingCommand as BeginIfCommand;
						var displayText = editedCommand.GetDisplayValue();
						var serializedData = JsonConvert.SerializeObject(editedCommand);

						selectedRow["Statement"] = displayText;
						selectedRow["CommandData"] = serializedData;
					}
				}
				else if (buttonSelected.Value.ToString() == "Delete")
				{
					//delete
					v_IfConditionsTable.Rows.Remove(selectedRow);
				}
				else
				{
					throw new NotImplementedException("Requested Action is not implemented.");
				}
			}
		}

		private void CreateIfCondition(object sender, EventArgs e)
		{
			var automationCommands = UIControlsHelper.GenerateCommandsandControls().Where(f => f.Command is BeginIfCommand).ToList();

			frmCommandEditor editor = new frmCommandEditor(automationCommands, null);
			editor.SelectedCommand = new BeginIfCommand();
			var res = editor.ShowDialog();

			if (res == DialogResult.OK)
			{
				//get data
				var configuredCommand = editor.SelectedCommand as BeginIfCommand;
				var displayText = configuredCommand.GetDisplayValue();
				var serializedData = JsonConvert.SerializeObject(configuredCommand);

				//add to list
				v_IfConditionsTable.Rows.Add(displayText, serializedData);
			}
		}

		private bool GetConditionResult(object sender)
		{
			bool isTrueStatement = true;
			foreach (DataRow rw in v_IfConditionsTable.Rows)
			{
				var commandData = rw["CommandData"].ToString();
				var ifCommand = JsonConvert.DeserializeObject<BeginIfCommand>(commandData);
				var statementResult = ifCommand.DetermineStatementTruth(sender);

				if (!statementResult)
				{
					isTrueStatement = false;
					break;
				}
			}
			return isTrueStatement;
		}
	}
}
