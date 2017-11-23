/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
/**
 * Class implementing basic services throughout YetaWF.
 */
var YetaWF_BasicsServices = (function () {
    function YetaWF_BasicsServices() {
        // JSX
        // JSX
        // JSX
        // WHENREADY
        // WHENREADY
        // WHENREADY
        /* TODO: This is public and push() is used to add callbacks (legacy Javascript ONLY) - Once transitioned, make whenReady private and remove $tag support */
        // Usage:
        // YetaWF_Basics.whenReady.push({
        //   callback: function($tag) {}    // function to be called
        // });
        //   or
        // YetaWF_Basics.whenReady.push({
        //   callbackTS: function(elem) {}    // function to be called
        // });
        this.whenReady = [];
        // WHENREADYONCE
        // WHENREADYONCE
        // WHENREADYONCE
        /* TODO: This is public and push() is used to add callbacks (legacy Javascript ONLY) - Once transitioned, make whenReadyOnce private and remove $tag support */
        // Usage:
        // YetaWF_Basics.whenReadyOnce.push({
        //   callback: function($tag) {}    // function to be called
        // });
        //   or
        // YetaWF_Basics.whenReadyOnce.push({
        //   callbackTS: function(elem) {}    // function to be called
        // });
        this.whenReadyOnce = [];
        // CLEARDIV
        // CLEARDIV
        // CLEARDIV
        this.clearDiv = [];
    }
    /**
     * React-like createElement function so we can use JSX in our TypeScript/JavaScript code.
     */
    YetaWF_BasicsServices.prototype.createElement = function (tag, attrs, children) {
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
    };
    /**
     * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
     * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
     * Callback functions are registered by whomever needs this type of processing. For example, a grid can
     * process all whenReady requests after reloading the grid with data (which doesn't run any javascript automatically).
     * @param def
     */
    YetaWF_BasicsServices.prototype.addWhenReady = function (callback) {
        this.whenReady.push({ callbackTS: callback });
    };
    //TODO: This should take an elem, not a jquery object
    /**
     * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
     * @param elem The element for which all callbacks should be called to initialize children.
     */
    YetaWF_BasicsServices.prototype.processAllReady = function ($tag) {
        if ($tag === undefined)
            $tag = $("body");
        for (var _i = 0, _a = this.whenReady; _i < _a.length; _i++) {
            var entry = _a[_i];
            try {
                if (entry.callback != null)
                    entry.callback($tag);
                else if (entry.callbackTS != null) {
                    $tag.each(function (ix, val) {
                        entry.callbackTS(val);
                    });
                }
            }
            catch (err) {
                console.log(err.message);
            }
        }
    };
    /**
     * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
     * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
     * Callback functions are registered by whomever needs this type of processing. For example, a grid can
     * process all whenReadyOnce requests after reloading the grid with data (which doesn't run any javascript automatically).
     * The callback is called for ONCE. Then the callback is removed.
     * @param def
     */
    YetaWF_BasicsServices.prototype.addWhenReadyOnce = function (callback) {
        this.whenReadyOnce.push({ callbackTS: callback });
    };
    //TODO: This should take an elem, not a jquery object
    /**
     * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
     * @param elem The element for which all callbacks should be called to initialize children.
     */
    YetaWF_BasicsServices.prototype.processAllReadyOnce = function ($tag) {
        if ($tag === undefined)
            $tag = $("body");
        for (var _i = 0, _a = this.whenReadyOnce; _i < _a.length; _i++) {
            var entry = _a[_i];
            try {
                if (entry.callback !== undefined)
                    entry.callback($tag);
                else {
                    $tag.each(function (ix, elem) {
                        entry.callbackTS(this);
                    });
                }
            }
            catch (err) {
                console.log(err.message);
            }
        }
        this.whenReadyOnce = [];
    };
    /**
     * Registers a callback that is called when a <div> is cleared. This is used so templates can register a cleanup
     * callback so elements can be destroyed when a div is emptied (used by UPS).
     */
    YetaWF_BasicsServices.prototype.addClearDiv = function (callback) {
        this.clearDiv.push({ callback: callback });
    };
    /**
     * Process all callbacks for the specified element being cleared. This is used by YetaWF.Core only.
     * @param elem The element being cleared.
     */
    YetaWF_BasicsServices.prototype.processClearDiv = function (tag) {
        for (var _i = 0, _a = this.clearDiv; _i < _a.length; _i++) {
            var entry = _a[_i];
            try {
                if (entry.callback != null)
                    entry.callback(tag);
            }
            catch (err) {
                console.log(err.message);
            }
        }
    };
    // SELECTORS
    // SELECTORS
    // SELECTORS
    // APIs to detach selectors from jQuery so this could be replaced with a smaller library (like sizzle).
    /**
     * Tests whether the specified element matches the selector.
     * @param elem - The element to test.
     * @param selector - The selector to match.
     */
    YetaWF_BasicsServices.prototype.elementMatches = function (elem, selector) {
        if (elem)
            return $(elem).is(selector);
        return false;
    };
    /**
     * Tests whether the specified element has the given css class.
     * @param elem The element to test.
     * @param css - The css class being tested.
     */
    YetaWF_BasicsServices.prototype.elementHasClass = function (elem, css) {
        css = css.trim();
        if (!elem)
            return false;
        if (elem.classList)
            return elem.classList.contains(css);
        else
            return new RegExp("(^| )" + css + "( |$)", "gi").test(elem.className);
    };
    // CONTENTCHANGE
    // CONTENTCHANGE
    // CONTENTCHANGE
    // APIs to detach custom event handling from jQuery so this could be replaced with a native mechanism
    YetaWF_BasicsServices.prototype.RegisterContentChange = function (callback) {
        $(document).on("YetaWF_Basics_Addon", function (event, addonGuid, on) { callback(event, addonGuid, on); });
    };
    // NEWPAGE
    // NEWPAGE
    // NEWPAGE
    // APIs to detach custom event handling from jQuery so this could be replaced with a native mechanism
    YetaWF_BasicsServices.prototype.RegisterNewPage = function (callback) {
        $(document).on("YetaWF_Basics_NewPage", function (event, url) { callback(event, url); });
    };
    return YetaWF_BasicsServices;
}());
/**
 * Basic services available throughout YetaWF.
 */
var YetaWF_Basics = new YetaWF_BasicsServices();

//# sourceMappingURL=Basics2.js.map
