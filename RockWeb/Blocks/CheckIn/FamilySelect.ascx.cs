﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Model;

namespace RockWeb.Blocks.CheckIn
{
    [DisplayName( "Family Select" )]
    [Category( "Check-in" )]
    [Description( "Displays a list of families to select for checkin." )]

    [TextField( "Title", "Title to display.", false, "Families", "Text", 5 )]
    [TextField( "Caption", "", false, "Select Your Family", "Text", 6 )]
    [TextField( "No Option Message", "", false, "Sorry, no one in your family is eligible to check-in at this location.", "Text", 7 )]
    public partial class FamilySelect : CheckInBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/checkin-core.js" );

            var bodyTag = this.Page.Master.FindControl( "bodyTag" ) as HtmlGenericControl;
            if ( bodyTag != null )
            {
                bodyTag.AddCssClass( "checkin-familyselect-bg" );
            }

            if ( CurrentWorkflow == null || CurrentCheckInState == null )
            {
                NavigateToHomePage();
            }
            else
            {
                if ( !Page.IsPostBack )
                {
                    ClearSelection();
                    lbAddFamily.Visible = CurrentCheckInState.Kiosk.RegistrationModeEnabled;
                    if ( CurrentCheckInState.CheckIn.Families.Count == 1 &&
                        !CurrentCheckInState.CheckIn.ConfirmSingleFamily )
                    {
                        if ( UserBackedUp )
                        {
                            GoBack();
                        }
                        else
                        {
                            foreach ( var family in CurrentCheckInState.CheckIn.Families )
                            {
                                family.Selected = true;
                            }

                            ProcessSelection();
                        }
                    }
                    else
                    {
                        lTitle.Text = GetAttributeValue( "Title" );
                        lCaption.Text = GetAttributeValue( "Caption" );

                        rSelection.DataSource = CurrentCheckInState.CheckIn.Families
                            .OrderBy( f => f.Caption )
                            .ThenBy( f => f.SubCaption )
                            .ToList();

                        rSelection.DataBind();
                    }
                }
                else
                {
                    if ( this.Request.Params["__EVENTTARGET"] == rSelection.UniqueID )
                    {
                        HandleRepeaterPostback( this.Request.Params["__EVENTARGUMENT"] );
                    }

                    if ( this.Request.Params["__EVENTARGUMENT"] == "EditFamily" )
                    {
                        hfShowEditFamilyPrompt.Value = "0";
                        var editFamilyBlock = this.RockPage.ControlsOfTypeRecursive<CheckInEditFamilyBlock>().FirstOrDefault();
                        if ( editFamilyBlock != null )
                        {
                            var firstListedFamily = this.CurrentCheckInState.CheckIn.Families.FirstOrDefault();
                            editFamilyBlock.ShowEditFamily( firstListedFamily );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear any previously selected families.
        /// </summary>
        private void ClearSelection()
        {
            foreach ( var family in CurrentCheckInState.CheckIn.Families )
            {
                family.Selected = false;
                family.People = new List<CheckInPerson>();
                family.Action = CheckinAction.CheckIn;
                family.CheckOutPeople = new List<CheckOutPerson>();
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rSelection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rSelection_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item == null )
            {
                return;
            }

            CheckInFamily checkInFamily = e.Item.DataItem as CheckInFamily;
            if ( checkInFamily == null )
            {
                return;
            }

            Panel pnlSelectFamilyPostback = e.Item.FindControl( "pnlSelectFamilyPostback" ) as Panel;
            pnlSelectFamilyPostback.Attributes["onclick"] = this.Page.ClientScript.GetPostBackClientHyperlink( rSelection, checkInFamily.Group.Id.ToString() );
            Literal lSelectFamilyButtonHtml = e.Item.FindControl( "lSelectFamilyButtonHtml" ) as Literal;
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, null, new Rock.Lava.CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
            mergeFields.Add( "Family", checkInFamily );
            mergeFields.Add( "Kiosk", CurrentCheckInState.Kiosk );
            mergeFields.Add( "RegistrationModeEnabled", CurrentCheckInState.Kiosk.RegistrationModeEnabled );

            var familySelectLavaTemplate = CurrentCheckInState.CheckInType.FamilySelectLavaTemplate;

            lSelectFamilyButtonHtml.Text = familySelectLavaTemplate.ResolveMergeFields( mergeFields );
        }

        /// <summary>
        /// Handles the repeater postback.
        /// </summary>
        /// <param name="commandArgument">The command argument.</param>
        protected void HandleRepeaterPostback( string commandArgument )
        {
            if ( KioskCurrentlyActive )
            {
                int groupId = commandArgument.AsInteger();
                var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Group.Id == groupId ).FirstOrDefault();
                if ( family != null )
                {
                    family.Selected = true;
                    ProcessSelection();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            GoBack();
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            CancelCheckin();
        }

        /// <summary>
        /// Special handling instead of the normal, default GoBack() behavior.
        /// </summary>
        protected override void GoBack()
        {
            if ( CurrentCheckInState != null && CurrentCheckInState.CheckIn != null )
            {
                CurrentCheckInState.CheckIn.SearchType = null;
                CurrentCheckInState.CheckIn.SearchValue = string.Empty;
                CurrentCheckInState.CheckIn.Families = new List<CheckInFamily>();
            }

            SaveState();

            if ( CurrentCheckInState.CheckIn.UserEnteredSearch )
            {
                NavigateToPreviousPage();
            }
            else
            {
                NavigateToHomePage();
            }
        }

        /// <summary>
        /// Gets the condition message.
        /// </summary>
        /// <value>
        /// The condition message.
        /// </value>
        protected string ConditionMessage
        {
            get
            {
                string conditionMessage = string.Format( "<p>{0}</p>", GetAttributeValue( "NoOptionMessage" ) );
                return conditionMessage;
            }
        }

        /// <summary>
        /// Processes the selection.
        /// </summary>
        private void ProcessSelection()
        {
            var editFamilyBlock = this.RockPage.ControlsOfTypeRecursive<CheckInEditFamilyBlock>().FirstOrDefault();

            hfShowEditFamilyPrompt.Value = "0";

            Func<bool> doNotProceedCondition = () =>
            {
                var noMatchingFamilies = CurrentCheckInState.CheckIn.Families.All( f => f.People.Count == 0 ) &&
                    CurrentCheckInState.CheckIn.Families.All( f => f.Action == CheckinAction.CheckIn );

                if ( noMatchingFamilies )
                {
                    if ( CurrentCheckInState.Kiosk.RegistrationModeEnabled && editFamilyBlock != null )
                    {
                        hfShowEditFamilyPrompt.Value = "1";
                        return true;
                    }
                    else
                    {
                        maWarning.Show( this.ConditionMessage, Rock.Web.UI.Controls.ModalAlertType.None );
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            };

            if ( !ProcessSelection( null, doNotProceedCondition, this.ConditionMessage ) )
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbAddFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddFamily_Click( object sender, EventArgs e )
        {
            var editFamilyBlock = this.RockPage.ControlsOfTypeRecursive<CheckInEditFamilyBlock>().FirstOrDefault();
            if ( editFamilyBlock != null )
            {
                editFamilyBlock.ShowAddFamily();
            }
        }
    }
}