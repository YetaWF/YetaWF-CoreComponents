/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/**
 * Implement a "fake" React.createElement function so we can use JSX in our TypeScript/JavaScript code.
 */
const React = {
    createElement: function (tag: string, attrs, children): HTMLElement {
        var element: HTMLElement = document.createElement(tag);
        for (let name in attrs) {
            if (name && attrs.hasOwnProperty(name)) {
                let value = attrs[name];
                if (value === true) {
                    element.setAttribute(name, name);
                } else if (value !== false && value != null) {
                    element.setAttribute(name, value.toString());
                }
            }
        }
        for (let i = 2; i < arguments.length; i++) {
            let child = arguments[i];
            element.appendChild(
                child.nodeType == null ?
                    document.createTextNode(child.toString()) : child);
        }
        return element;
    }
};

