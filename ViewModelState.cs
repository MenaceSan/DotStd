using System;
using System.Collections.Generic;

namespace DotStd
{
    /// <summary>
    /// What type of access/change was this ? For the change audits.
    /// </summary>
    public enum ChangeType
    {
        Error = 0,      // Failure, dont use this.
        View = 1,       // no changes. Just a view of this data. for HIPAA audit trail.
        Modify = 2,     // Made a change to some field(s) in this record. AKA Change/Edit/Update
        Add = 3,        // new record created. AKA Insert.
        Delete = 4,
        MaxValue = 5,
    };

    /// <summary>
    /// Presentation Access Level for a single property/field of some object.
    /// For a given view/context the Props values will have different exposure/access.
    /// </summary>
    [Serializable()]
    public enum ViewPropertyAccess
    {
        Ignored = 0,        // Pretend this Prop doesn't exist. delete from _aProps ?
        Hidden = 1,         // hidden and left with default value = just omit Prop for this. Assume null or default value. used for internal purposes by the form.
        ShowNoEdit = 2,     // show but not editable. Read Only. Disable Edit.  Allow null or default value.
        OptionalEdit = 3,   // shown and Optional to edit, but this can be omitted/null/defaulted.
        Required = 4,       // shown and must contain a valid value. Therefore it must be editable and have a valid value. not null.
    }

    /// <summary>
    /// The status of properties of a ViewModel object _ObjectInstance shown in the GUI (usually as a dialog). 
    /// define access level to Props in _ObjectInstance we show,use,etc. for the current user and form.
    /// Track dirty/changed vs. unchanged status of Props values.
    /// </summary>
    [Serializable()]
    public class ViewModelState
    {
        class ViewProperty
        {
            public string MemberName { get; set; }    //! Property name (must match reflection name of property in _ObjectInstance)
            public ViewPropertyAccess Level { get; set; }   //! Access for the current user and form.

            public ViewProperty(string MemberName, ViewPropertyAccess Level)
            {
                this.MemberName = MemberName;
                this.Level = Level;
            }
        }

        private readonly object _ObjectInstance;    //! What view model object are we tracking? usually based on IValidatable
        private List<ViewProperty> _aProps = new List<ViewProperty>();  //! What access to Props do we have in _ObjectInstance?
        private List<string>? _aOrigValues;          //! _ObjectInstance.Property.ToString() encoded state of the Props at start/clean state. same enum as _aProps

        public ViewModelState(object o, bool bTrackChangedValues = true, ViewPropertyAccess nLevelDef = ViewPropertyAccess.Ignored)
        {
            _ObjectInstance = o;   // ASSUME not null.
            if (bTrackChangedValues)
                _aOrigValues = new List<string>();
            else
                _aOrigValues = null; // must call UpdateOrigValues() later if i care about tracking changed values. IsChanged()
            if (nLevelDef > ViewPropertyAccess.Ignored)
            {
                SetPropAccessAll(nLevelDef);
            }
        }

        /// <summary>
        /// Are we tracking this prop of _ObjectInstance? use nameof(MemberName).
        /// </summary>
        /// <param name="memberName"></param>
        /// <returns>-1 = no</returns>
        public int GetPropIndex(string memberName)
        {
            int i = 0;
            foreach (ViewProperty p in _aProps)
            {
                if (memberName == p.MemberName)
                    return i;
                i++;
            }
            return -1; // no such sPropName.
        }

        /// <summary>
        /// Via reflection get the current value of a sPropName on _ObjectInstance.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string GetPropValue(string propertyName)
        {
            object? o = PropertyUtil.GetPropertyValue(_ObjectInstance, propertyName);
            if (o == null)
                return string.Empty;
            return o.ToString() ?? string.Empty;
        }

        /// <summary>
        /// What access level is this sPropName? for current user/form.
        /// like GetPropIndex
        /// </summary>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public ViewPropertyAccess GetPropAccess(string memberName)
        {
            foreach (ViewProperty p in _aProps)
            {
                if (memberName == p.MemberName)
                    return p.Level;
            }
            return ViewPropertyAccess.Ignored; // no such sPropName.
        }

        public void SetPropAccess(string memberName, ViewPropertyAccess nLevel)
        {
            //! What access do we want to give to this sPropName? for current user/form.
            int i = GetPropIndex(memberName);
            if (i >= 0) // modify existing?
            {
                _aProps[i].Level = nLevel;
            }
            else // insert
            {
                _aProps.Add(new ViewProperty(memberName, nLevel));
                if (_aOrigValues != null)
                {
                    // store current value.
                    _aOrigValues.Add(GetPropValue(memberName));
                }
            }
        }

        public void SetPropAccesses(string[] memberNames, ViewPropertyAccess nLevel)
        {
            //! Add access for a list of props in _ObjectInstance.
            foreach (string memberName in memberNames)
            {
                SetPropAccess(memberName, nLevel);
            }
        }

        public void SetPropAccessAll(ViewPropertyAccess nLevel = ViewPropertyAccess.OptionalEdit)
        {
            //! add default access to all props of _ObjectInstance. for simple POCO DTO type objects.
            //! we can add/remove/change these props later of course.
            Type oType = _ObjectInstance.GetType();
            foreach (var prop in oType.GetProperties())
            {
                SetPropAccess(prop.Name, nLevel);
            }
        }

        public void UpdateOrigValues()
        {
            //! Grab _ObjectInstance values for all _aProps and store in _aOrigValues
            //! Make IsChanged = false
            _aOrigValues = new List<string>();
            foreach (ViewProperty p in _aProps)
            {
                if (p.Level <= ViewPropertyAccess.Ignored)   // Don't bother filling these.
                    continue;
                _aOrigValues.Add(GetPropValue(p.MemberName));
            }
        }
        public bool IsChanged(string memberName)
        {
            //! Has memberName (in _ObjectInstance) value changed?
            //! NOTE: _aOrigValues = null means UpdateOrigValues was never called. Fail. We are NOT allowed to call this function.
            if (_aOrigValues == null)
                return false;
            int i = GetPropIndex(memberName);
            if (i < 0)  // not listed.
                return false;
            if (_aProps[i].Level <= ViewPropertyAccess.Ignored)
                return false;
            if (_aOrigValues[i] != GetPropValue(memberName))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Has anything changed?
        /// NOTE: _aOrigValues = null means UpdateOrigValues was never called. Fail. We are NOT allowed to call this function.
        /// </summary>
        /// <returns></returns>
        public bool IsChangedAny()
        {
            if (_aOrigValues == null)
                return false;
            int i = 0;
            foreach (ViewProperty p in _aProps)
            {
                if (p.Level > ViewPropertyAccess.Ignored
                    && _aOrigValues[i] != GetPropValue(p.MemberName))
                    return true;
                i++;
            }
            return false;
        }

        /// <summary>
        /// List all props that have changed.
        /// NOTE: _aOrigValues = null means UpdateOrigValues was never called. Fail. We are NOT allowed to call this function.
        /// </summary>
        /// <returns></returns>
        List<string> GetChangedProps()
        {
            var aChanged = new List<string>();
            if (_aOrigValues == null)
                return aChanged;

            int i = 0;
            foreach (ViewProperty p in _aProps)
            {
                if (p.Level > ViewPropertyAccess.Ignored
                    && _aOrigValues[i] != GetPropValue(p.MemberName))
                    aChanged.Add(p.MemberName);
                i++;
            }
            return aChanged;
        }
    }
}
