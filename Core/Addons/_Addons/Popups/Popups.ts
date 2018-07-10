/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* TODO : While transitioning to TypeScript and to maintain compatibility with all plain JavaScript, these defs are all global rather than in their own namespace.
   Once the transition is complete, we need to revisit this */

/* Popups API, to be implemented by rendering-specific code - rendering code must define a YetaWF_PopupsImpl object implementing IPopupsImpl */

/**
    Implemented by custom rendering.
 */
declare var YetaWF_PopupsImpl: YetaWF.IPopupsImpl;

/**
 * Class implementing popup services used throughout YetaWF.
 */
namespace YetaWF {

    export interface IPopupsImpl {

        /**
         * Close the popup - this can only be used by code that is running within the popup (not the parent document/page)
         */
        closePopup(forceReload?: boolean) : void;

        /**
         * Close the popup - this can only be used by code that is running on the main page (not within the popup)
         */
        closeInnerPopup(): void;

        /**
         * Opens a dynamic popup, usually a div added to the current document.
         */
        openDynamicPopup(result: ContentResult): HTMLElement;

        /**
         * Open a static popup, usually a popup based on iframe.
         */
        openStaticPopup(url: string): void;
    }

    export interface IVolatile {
        Popups: IVolatilePopups;
    }
    export interface IVolatilePopups {
        AllowPopups: boolean;
    }

    export interface IConfigs {
        Popups: IConfigsPopups;
    }
    export interface IConfigsPopups {
        DefaultPopupWidth: number;
        DefaultPopupHeight: number;
    }

    export class PopupsServices {

        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer

        /**
         * Close the popup - this can only be used by code that is running within the popup (not the parent document/page)
         */
        public closePopup(forceReload?: boolean): void {
            YetaWF_PopupsImpl.closePopup(forceReload);
        }

        /**
         * Close the popup - this can only be used by code that is running on the main page (not within the popup)
         */
        public closeInnerPopup(): void {
            YetaWF_PopupsImpl.closeInnerPopup();
        }

        // Implemented by YetaWF
        // Implemented by YetaWF
        // Implemented by YetaWF

        /**
         * opens a popup given a url
         */
        public openPopup(url: string, forceIframe: boolean): boolean {

            YetaWF_Basics.setLoading(true);

            // build a url that has a random portion so the page is not cached - this is so we can have the same page nested within itself
            if (url.indexOf('?') < 0)
                url += '?';
            else
                url += "&";
            url += new Date().getUTCMilliseconds();
            url += "&" + YConfigs.Basics.Link_ToPopup + "=y";// we're now going into a popup

            if (!forceIframe) {
                if (YetaWF_Basics.ContentHandling.setContent(new URI(url), false, YetaWF_PopupsImpl.openDynamicPopup)) {
                    // contents set in dynamic popup
                    return true;
                }
            }
            YetaWF_PopupsImpl.openStaticPopup(url);
            return true;
        }

        /**
         * Handles links that invoke a popup window.
         */
        public handlePopupLink(elem: HTMLAnchorElement): boolean {

            var url = elem.href;

            // check if this is a popup link
            if (!YetaWF_Basics.elementHasClass(elem, YConfigs.Basics.CssPopupLink))
                return false;
            // check whether we allow popups at all
            if (!YVolatile.Popups.AllowPopups)
                return false;
            if (YVolatile.Skin.MinWidthForPopups > window.outerWidth) // the screen is too small for a popup
                return false;
            // if we're switching from https->http or from http->https don't use a popup
            if (!url.startsWith("http") || !window.document.location.href.startsWith("http"))
                return false;
            if ((url.startsWith("http://") != window.document.location.href.startsWith("http://")) ||
                (url.startsWith("https://") != window.document.location.href.startsWith("https://")))
                return false;
            if (YVolatile.Basics.EditModeActive || YVolatile.Basics.PageControlVisible) {
                //if we're in edit mode or the page control module is visible, all links bring up a page (no popups) except for modules with the PopupEdit style
                if (!elem.getAttribute(YConfigs.Basics.CssAttrDataSpecialEdit))
                    return false;
            }

            return YetaWF_Popups.openPopup(url, false);
        };

        /**
         * Handles links in a popup that link to a url in the outer parent (main) window.
         */
        public handleOuterWindow(elem: HTMLAnchorElement): boolean {
            // check if this is a popup link
            if (!elem.getAttribute(YConfigs.Basics.CssOuterWindow))
                return false;
            if (!YetaWF_Basics.isInPopup()) return false; // this shouldn't really happen
            YetaWF_Basics.setLoading(true);
            if (!window.parent.YetaWF_Basics.ContentHandling.setContent(new URI(elem.href), true))
                window.parent.location.assign(elem.href);
            return true;
        };
    }
}

/**
 * Popup services available throughout YetaWF.
 */
var YetaWF_Popups: YetaWF.PopupsServices = new YetaWF.PopupsServices();
