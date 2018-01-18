/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;

namespace YetaWF.Core.Models.Attributes {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ResourceRedirectAttribute : Attribute {
        public ResourceRedirectAttribute(string property, int index, string resourceObject) {
            Property = property;
            Index = index;
            ResourceObject = resourceObject;
        }
        public string Property { get; private set; }
        public int Index { get; private set; }
        public string ResourceObject { get; private set; }

        public string GetCaption(object parentObj) {
            PropertyData propData = ObjectSupport.GetPropertyData(parentObj.GetType(), Property);// retrieve the List
            object resObject = propData.PropInfo.GetValue(parentObj); // get the property value
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Count"); // prepare to retrieve the number of items
            int count = (int) propData.PropInfo.GetValue(resObject); // get the list count
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Item"); // prepare to retrieve a specific item
            if (count <= Index)
                return "";
            resObject = propData.PropInfo.GetValue(resObject, new object[] { Index }); // get the list entry
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), ResourceObject);// retrieve the Resource Object property
            resObject = propData.PropInfo.GetValue(resObject);
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Caption");// retrieve the Resource Object's Caption property
            return (string) propData.PropInfo.GetValue(resObject);
        }
        public string GetDescription(object parentObj) {
            PropertyData propData = ObjectSupport.GetPropertyData(parentObj.GetType(), Property);// retrieve the List
            object resObject = propData.PropInfo.GetValue(parentObj); // get the property value
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Count"); // prepare to retrieve the number of items
            int count = (int) propData.PropInfo.GetValue(resObject); // get the list count
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Item"); // prepare to retrieve a specific item
            if (count <= Index)
                return "";
            resObject = propData.PropInfo.GetValue(resObject, new object[] { Index }); // get the list entry
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), ResourceObject);// retrieve the Resource Object property
            resObject = propData.PropInfo.GetValue(resObject);
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Description");// retrieve the Resource Object's Description property
            return (string) propData.PropInfo.GetValue(resObject);
        }
        public string GetHelpLink(object parentObj) {
            PropertyData propData = ObjectSupport.GetPropertyData(parentObj.GetType(), Property);// retrieve the List
            object resObject = propData.PropInfo.GetValue(parentObj); // get the property value
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Count"); // prepare to retrieve the number of items
            int count = (int) propData.PropInfo.GetValue(resObject); // get the list count
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Item"); // prepare to retrieve a specific item
            if (count <= Index)
                return "";
            resObject = propData.PropInfo.GetValue(resObject, new object[] { Index }); // get the list entry
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), ResourceObject);// retrieve the Resource Object property
            resObject = propData.PropInfo.GetValue(resObject);
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "HelpLink");// retrieve the Resource Object's HelpLink property
            return (string) propData.PropInfo.GetValue(resObject);
        }
    }
}
