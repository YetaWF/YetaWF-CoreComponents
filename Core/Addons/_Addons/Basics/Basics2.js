/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
/**
 * Implement a "fake" React.createElement function so we can use JSX in our TypeScript/JavaScript code.
 */
var React = {
    createElement: function (tag, attrs, children) {
        var element = document.createElement(tag);
        for (var name_1 in attrs) {
            if (name_1 && attrs.hasOwnProperty(name_1)) {
                var value = attrs[name_1];
                if (value === true) {
                    element.setAttribute(name_1, name_1);
                }
                else if (value !== false && value != null) {
                    element.setAttribute(name_1, value.toString());
                }
            }
        }
        for (var i = 2; i < arguments.length; i++) {
            var child = arguments[i];
            element.appendChild(child.nodeType == null ?
                document.createTextNode(child.toString()) : child);
        }
        return element;
    }
};
//# sourceMappingURL=Basics2.js.map