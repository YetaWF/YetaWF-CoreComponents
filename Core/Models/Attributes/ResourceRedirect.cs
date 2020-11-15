/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ResourceRedirectListAttribute : Attribute {

        public ResourceRedirectListAttribute(string property, int index, string resourceObject) {
            Property = property;
            Index = index;
            ResourceObject = resourceObject;
        }
        public string Property { get; private set; }
        public int Index { get; private set; }
        public string ResourceObject { get; private set; }

        public string? GetCaption(object container) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), Property);// retrieve the List
            object? resObject = propData.PropInfo.GetValue(container); // get the property value
            if (resObject == null) throw new InternalError($"null value for property {Property}");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Count"); // prepare to retrieve the number of items
            int count = (int) propData.PropInfo.GetValue(resObject) !; // get the list count
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Item"); // prepare to retrieve a specific item
            if (count <= Index)
                return string.Empty;
            resObject = propData.PropInfo.GetValue(resObject, new object[] { Index }); // get the list entry
            if (resObject == null) throw new InternalError($"null value for property {Property} index");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), ResourceObject);// retrieve the Resource Object property
            resObject = propData.PropInfo.GetValue(resObject);
            if (resObject == null) throw new InternalError($"null value for property {Property}[index]");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Caption");// retrieve the Resource Object's Caption property
            return (string?) propData.PropInfo.GetValue(resObject);
        }
        public string? GetDescription(object container) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), Property);// retrieve the List
            object? resObject = propData.PropInfo.GetValue(container); // get the property value
            if (resObject == null) throw new InternalError($"null value for property {Property}");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Count"); // prepare to retrieve the number of items
            int count = (int) propData.PropInfo.GetValue(resObject) !; // get the list count
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Item"); // prepare to retrieve a specific item
            if (count <= Index)
                return string.Empty;
            resObject = propData.PropInfo.GetValue(resObject, new object[] { Index }); // get the list entry
            if (resObject == null) throw new InternalError($"null value for property {Property} index");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), ResourceObject);// retrieve the Resource Object property
            resObject = propData.PropInfo.GetValue(resObject);
            if (resObject == null) throw new InternalError($"null value for property {Property}[index]");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Description");// retrieve the Resource Object's Description property
            return (string?) propData.PropInfo.GetValue(resObject);
        }
        public string? GetHelpLink(object container) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), Property);// retrieve the List
            object? resObject = propData.PropInfo.GetValue(container); // get the property value
            if (resObject == null) throw new InternalError($"null value for property {Property}");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Count"); // prepare to retrieve the number of items
            int count = (int) propData.PropInfo.GetValue(resObject) !; // get the list count
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "Item"); // prepare to retrieve a specific item
            if (count <= Index)
                return string.Empty;
            resObject = propData.PropInfo.GetValue(resObject, new object[] { Index }); // get the list entry
            if (resObject == null) throw new InternalError($"null value for property {Property} index");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), ResourceObject);// retrieve the Resource Object property
            resObject = propData.PropInfo.GetValue(resObject);
            if (resObject == null) throw new InternalError($"null value for property {Property}[index]");
            propData = ObjectSupport.GetPropertyData(resObject.GetType(), "HelpLink");// retrieve the Resource Object's HelpLink property
            return (string?) propData.PropInfo.GetValue(resObject);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ResourceRedirectAttribute : Attribute {

        public ResourceRedirectAttribute(string? propertyCaption, string? propertyDescription = null, string? propertyHelpLink = null) {
            PropertyCaption = propertyCaption;
            PropertyDescription = propertyDescription;
            PropertyHelpLink = propertyHelpLink;
        }
        public string? PropertyCaption { get; private set; }
        public string? PropertyDescription { get; private set; }
        public string? PropertyHelpLink { get; private set; }

        public string? GetCaption(object container) {
            if (PropertyCaption == null) return null;
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), PropertyCaption);
            return (string?)propData.PropInfo.GetValue(container);
        }
        public string? GetDescription(object container) {
            if (PropertyDescription == null) return null;
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), PropertyDescription);
            return (string?)propData.PropInfo.GetValue(container);
        }
        public string? GetHelpLink(object container) {
            if (PropertyHelpLink == null) return null;
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), PropertyHelpLink);
            return (string?)propData.PropInfo.GetValue(container);
        }
    }
}
