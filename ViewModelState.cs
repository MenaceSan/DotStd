using System;
using System.Collections.Generic;

namespace DotStd
{
    public enum ChangeType
    {
        // For the change audits.
        Error = -1,
        None = 0,   // no changes.
        Modify = 1,
        Add = 2,
        Delete = 3,
    };

    [Serializable()]
    public enum ViewPropertyAccess
    {
        // Presentation Access Level for a single property/field of some object.
        // For a given view/context the Props values will have different exposure/access.
        Ignored = 0,        // Pretend this Prop doesn't exist. delete from _aProps ?
        Hidden = 1,         // hidden and left with default value = just omit Prop for this. Assume null or default value. used for internal purposes by the form.
        ShowNoEdit = 2,     // show but not editable. Read Only. Disable Edit.  Allow null or default value.
        OptionalEdit = 3,   // shown and Optional to edit, but this can be omitted/null/defaulted.
        Required = 4,       // shown and must contain a valid value. Therefore it must be editable and have a valid value. not null.
    }

    [Serializable()]
    public class ViewModelState
    {
        //! The status of properties of a ViewModel object _ObjectInstance shown in the GUI (usually as a dialog). 
        //! define access level to Props in _ObjectInstance we show,use,etc. for the current user and form.
        //! Track dirty/changed vs. unchanged status of Props values.

        class ViewProperty
        {
            //! Child of ViewModelState
            public string MemberName { get; set; }    //! Property name (must match reflection name of property in _ObjectInstance)
            public ViewPropertyAccess Level { get; set; }   //! Access for the current user and form.
        }

        private readonly object _ObjectInstance;    //! What view model object are we tracking? usually based on IValidatable
        private List<ViewProperty> _aProps;         //! What access to Props do we have in _ObjectInstance?
        private List<string> _aOrigValues;          //! _ObjectInstance.Property.ToString() encoded state of the Props at start/clean state. same enum as _aProps

        public ViewModelState(object o, bool bTrackChangedValues = true, ViewPropertyAccess nLevelDef = ViewPropertyAccess.Ignored)
        {
            _ObjectInstance = o;   // ASSUME not null.
            _aProps = new List<ViewProperty>();
            if (bTrackChangedValues)
                _aOrigValues = new List<string>();
            else
                _aOrigValues = null; // must call UpdateOrigValues() later if i care about tracking changed values. IsChanged()
            if (nLevelDef > ViewPropertyAccess.Ignored)
            {
                SetPropAccessAll(nLevelDef);
            }
        }

        public int GetPropIndex(string memberName)
        {
            //! Are we tracking this prop of _ObjectInstance? use nameof(MemberName).
            //! @return -1 = no
            int i =0;
            foreach (ViewProperty p in _aProps)
            {
                if (memberName == p.MemberName)
                    return i;
                i++;
            }
            return -1; // no such sPropName.
        }

        private string GetPropValue(System.Reflection.PropertyInfo p)
        {
            //! Via reflection get the current value of a sPropName on _ObjectInstance.
            if (p == null)
                return null;
            object oValue = p.GetValue(_ObjectInstance, null);
            return oValue.ToString();
        }
        public string GetPropValue(string memberName)
        {
            //! Via reflection get the current value of a sPropName on _ObjectInstance.
            Type oType = _ObjectInstance.GetType();
            return GetPropValue(oType.GetProperty(memberName));
        }

        public ViewPropertyAccess GetPropAccess(string memberName)
        {
            //! What access level is this sPropName? for current user/form.
            //! like GetPropIndex
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
                _aProps.Add(new ViewProperty { MemberName = memberName, Level = nLevel });
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

        public bool IsChangedAny()
        {
            // Has anything changed?
            // NOTE: _aOrigValues = null means UpdateOrigValues was never called. Fail. We are NOT allowed to call this function.
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

        List<string> GetChangedProps()
        {
            // List all props that have changed.
            // NOTE: _aOrigValues = null means UpdateOrigValues was never called. Fail. We are NOT allowed to call this function.

            ValidateArgument.EnsureNotNull(_aOrigValues,nameof(_aOrigValues));

            var aChanged = new List<string>();
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
