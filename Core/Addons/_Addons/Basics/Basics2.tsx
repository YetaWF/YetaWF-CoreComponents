﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* TODO: While transitioning to TypeScript and to maintain compatibility with all plain JavaScript, these defs are all global rather than in their own namespace.
   Once the transition is complete, we need to revisit this */

interface IWhenReady {
    /**
     * @deprecated
     */
    callback?($tag: JQuery<HTMLElement>): void;

    callbackTS?(elem: HTMLElement): void;
}

/**
 * Class implementing basic services throughout YetaWF.
 */
class YetaWF_BasicsServices {

    // JSX
    // JSX
    // JSX

    /**
     * React-like createElement function so we can use JSX in our TypeScript/JavaScript code.
     */
    public createElement (tag: string, attrs: any, children: any): HTMLElement {
        var element: HTMLElement = document.createElement(tag);
        for (let name in attrs) {
            if (name && attrs.hasOwnProperty(name)) {
                var value: string | null | boolean = attrs[name];
                if (value === true) {
                    element.setAttribute(name, name);
                } else if (value !== false && value != null) {
                    element.setAttribute(name, value.toString());
                }
            }
        }
        for (let i:number = 2; i < arguments.length; i++) {
            let child:any = arguments[i];
            element.appendChild(
                child.nodeType == null ?
                    document.createTextNode(child.toString()) : child);
        }
        return element;
    }

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
    public whenReady: IWhenReady[] = [];

    /**
     * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
     * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
     * Callback functions are registered by whomever needs this type of processing. For example, a grid can
     * process all whenReady requests after reloading the grid with data (which doesn't run any javascript automatically).
     * @param def
     */
    public addWhenReady(callback: (section: HTMLElement) => void): void {
        this.whenReady.push({ callbackTS: callback });
    }

    //TODO: This should take an elem, not a jquery object
    /**
     * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
     * @param elem The element for which all callbacks should be called to initialize children.
     */
    public processAllReady($tag: JQuery<HTMLElement>): void {
        if ($tag === undefined) $tag = $("body");
        for (var entry of this.whenReady) {
            try { // catch errors to insure all callbacks are called
                if (entry.callback != null)
                    entry.callback($tag);
                else if (entry.callbackTS != null) {
                    $tag.each(function (ix:number, val: HTMLElement):void {
                        (entry.callbackTS as any)(val);
                    });
                }
            } catch (err) {
                console.log(err.message);
            }
        }
    }

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
    public whenReadyOnce: IWhenReady[] = [];

    /**
     * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
     * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
     * Callback functions are registered by whomever needs this type of processing. For example, a grid can
     * process all whenReadyOnce requests after reloading the grid with data (which doesn't run any javascript automatically).
     * The callback is called for ONCE. Then the callback is removed.
     * @param def
     */
    public addWhenReadyOnce(callback: (section: HTMLElement) => void): void {
        this.whenReadyOnce.push({ callbackTS: callback });
    }

    //TODO: This should take an elem, not a jquery object
    /**
     * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
     * @param elem The element for which all callbacks should be called to initialize children.
     */
    public processAllReadyOnce($tag: JQuery<HTMLElement>): void {
        if ($tag === undefined) $tag = $("body");
        for (var entry of this.whenReadyOnce) {
            try { // catch errors to insure all callbacks are called
                if (entry.callback !== undefined)
                    entry.callback($tag);
                else {
                    $tag.each(function (ix: number, elem: HTMLElement): void {
                        (entry.callbackTS as any)(this);
                    });
                }
            } catch (err) {
                console.log(err.message);
            }
        }
        this.whenReadyOnce = [];
    }
}


/**
 * Basic services available throughout YetaWF.
 */
var YetaWF_Basics: YetaWF_BasicsServices = new YetaWF_BasicsServices();
