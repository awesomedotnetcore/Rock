﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.Utilities;

namespace Rock.Reporting.DataFilter.BenevolenceResult
{
    /// <summary>
    ///     A Data Filter to select Benevolence Results by their Benevolence Requests from a Benevolence Request View.
    /// </summary>
    [Description( "Select Benevolence Results by their Benevolence Requests from a Benevolence Request View." )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "Benevolence Request View" )]
    public class BenevolenceRequestDataViewFilter : DataFilterComponent
    {
        #region Settings

        /// <summary>
        ///     Settings for the Data Select Component.
        /// </summary>
        private class FilterSettings : SettingsStringBase
        {
            public Guid? DataViewGuid;

            public FilterSettings()
            {
                //
            }

            public FilterSettings( string settingsString )
            {
                FromSelectionString( settingsString );
            }

            /// <summary>
            /// Indicates if the current settings are valid.
            /// </summary>
            /// <value>
            /// True if the settings are valid.
            /// </value>
            public override bool IsValid
            {
                get
                {
                    return DataViewGuid.HasValue;
                }
            }

            /// <summary>
            /// Set the property values parsed from a settings string.
            /// </summary>
            /// <param name="version">The version number of the parameter set.</param>
            /// <param name="parameters">An ordered collection of strings representing the parameter values.</param>
            protected override void OnSetParameters( int version, IReadOnlyList<string> parameters )
            {
                // Parameter 1: Data View
                DataViewGuid = DataComponentSettingsHelper.GetParameterOrEmpty( parameters, 0 ).AsGuidOrNull();

            }

            /// <summary>
            /// Gets an ordered set of property values that can be used to construct the
            /// settings string.
            /// </summary>
            /// <returns>
            /// An ordered collection of strings representing the parameter values.
            /// </returns>
            protected override IEnumerable<string> OnGetParameters()
            {
                var settings = new List<string>();

                settings.Add( DataViewGuid.ToStringSafe() );
                return settings;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the entity type that the filter applies to.
        /// </summary>
        /// <value>
        /// The namespace-qualified Type name of the entity that the filter applies to, or an empty string if the filter applies to all entities.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof( Model.BenevolenceResult ).FullName; }
        }

        /// <summary>
        /// Gets the name of the section in which the filter should be displayed in a browsable list.
        /// </summary>
        /// <value>
        /// The section name.
        /// </value>
        public override string Section
        {
            get { return "Related Data Views"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the user-friendly title used to identify the filter component.
        /// </summary>
        /// <param name="entityType">The System Type of the entity to which the filter will be applied.</param>
        /// <returns>
        /// The name of the filter.
        /// </returns>
        public override string GetTitle( Type entityType )
        {
            return "Benevolence Request Data View";
        }

        /// <summary>
        ///     Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        ///     will set the description of the filter to whatever is returned by this property.  If including script, the
        ///     controls parent container can be referenced through a '$content' variable that is set by the control before
        ///     referencing this property.
        /// </summary>
        /// <value>
        ///     The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return @"
function() {
  var dataViewName = $('.rock-drop-down-list,select:first', $content).find(':selected').text();
  var BenevolenceRequest = $('.rock-drop-down-list,select:last', $content).find(':selected').text();
  var result = 'BenevolenceRequest';
  if (BenevolenceRequest) {
     result = result + ' type ""' + BenevolenceRequest + "";
  }  
  result = result + ' is in filter: ' + dataViewName;
  return result;
}
";
        }

        /// <summary>
        /// Provides a user-friendly description of the specified filter values.
        /// </summary>
        /// <param name="entityType">The System Type of the entity to which the filter will be applied.</param>
        /// <param name="selection">A formatted string representing the filter settings.</param>
        /// <returns>
        /// A string containing the user-friendly description of the settings.
        /// </returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            var settings = new FilterSettings( selection );

            string result = "Connected to Benevolence Request";

            if ( !settings.IsValid )
            {
                return result;
            }

            using ( var context = new RockContext() )
            {
                var dataView = new DataViewService( context ).Get( settings.DataViewGuid.GetValueOrDefault() );

                result = string.Format( "Benevolence Result in Data View \"{0}\"", ( dataView != null ? dataView.ToString() : string.Empty ) );
            }

            return result;
        }

        private const string _CtlDataView = "dvpDataView";

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="filterControl">The control that serves as the container for the filter controls.</param>
        /// <returns>
        /// The array of new controls created to implement the filter.
        /// </returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            var dvpDataView = new DataViewItemPicker();
            dvpDataView.ID = filterControl.GetChildControlInstanceName( _CtlDataView );
            dvpDataView.Label = "Has Benevolence Request in this Data View";
            dvpDataView.Help = "A Data View that provides the set of Benevolence Request to match.";

            filterControl.Controls.Add( dvpDataView );

            // Populate the Data View Picker
            int entityTypeId = EntityTypeCache.Get( typeof( Model.BenevolenceRequest ) ).Id;
            dvpDataView.EntityTypeId = entityTypeId;

            return new Control[] { dvpDataView };
        }

        /// <summary>
        /// Gets the selection.
        /// Implement this version of GetSelection if your DataFilterComponent works the same in all FilterModes
        /// </summary>
        /// <param name="entityType">The System Type of the entity to which the filter will be applied.</param>
        /// <param name="controls">The collection of controls used to set the filter values.</param>
        /// <returns>
        /// A formatted string.
        /// </returns>
        public override string GetSelection( Type entityType, Control[] controls )
        {
            var dvpDataView = controls.GetByName<DataViewItemPicker>( _CtlDataView );

            var settings = new FilterSettings();

            settings.DataViewGuid = DataComponentSettingsHelper.GetDataViewGuid( dvpDataView.SelectedValue );

            return settings.ToSelectionString();
        }

        /// <summary>
        /// Sets the selection.
        /// Implement this version of SetSelection if your DataFilterComponent works the same in all FilterModes
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            var dvpDataView = controls.GetByName<DataViewItemPicker>( _CtlDataView );

            var settings = new FilterSettings( selection );

            if ( !settings.IsValid )
            {
                return;
            }

            dvpDataView.SetValue( DataComponentSettingsHelper.GetDataViewId( settings.DataViewGuid ) );
        }

        /// <summary>
        /// Creates a Linq Expression that can be applied to an IQueryable to filter the result set.
        /// </summary>
        /// <param name="entityType">The type of entity in the result set.</param>
        /// <param name="serviceInstance">A service instance that can be queried to obtain the result set.</param>
        /// <param name="parameterExpression">The input parameter that will be injected into the filter expression.</param>
        /// <param name="selection">A formatted string representing the filter settings.</param>
        /// <returns>
        /// A Linq Expression that can be used to filter an IQueryable.
        /// </returns>
        /// <exception cref="System.Exception">Filter issue(s):  + errorMessages.AsDelimited( ;  )</exception>
        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            var settings = new FilterSettings( selection );

            var context = ( RockContext ) serviceInstance.Context;

            // Get the Benevolence Request Data View.
            var dataView = DataComponentSettingsHelper.GetDataViewForFilterComponent( settings.DataViewGuid, context );

            // Evaluate the Data View that defines the Benevolence Request.
            var benevolenceRequestService = new BenevolenceRequestService( context );

            var benevolenceRequestQuery = benevolenceRequestService.Queryable();

            if ( dataView != null )
            {
                benevolenceRequestQuery = DataComponentSettingsHelper.FilterByDataView( benevolenceRequestQuery, dataView, benevolenceRequestService );
            }

            var benevolenceRequestKey = benevolenceRequestQuery.Select( a => a.Id );
            // Get all of the result corresponding to the qualifying Benevolence Requests.
            var qry = new BenevolenceResultService( context ).Queryable()
                                                  .Where( g => benevolenceRequestKey.Contains( g.BenevolenceRequestId ) );

            // Retrieve the Filter Expression.
            var extractedFilterExpression = FilterExpressionExtractor.Extract<Model.BenevolenceResult>( qry, parameterExpression, "g" );

            return extractedFilterExpression;
        }

        #endregion

    }
}
