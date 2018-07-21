﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* TODO: While transitioning to TypeScript and to maintain compatibility with all plain JavaScript, these defs are all global rather than in their own namespace.
   Once the transition is complete, we need to revisit this */

declare var YVolatile: YetaWF.IVolatile;
declare var YConfigs: YetaWF.IConfigs;
declare var YLocs: YetaWF.ILocs;

namespace YetaWF {

    // VOLATILE
    // VOLATILE
    // VOLATILE

    export interface IVolatile {
        Basics: IVolatileBasics;
        Skin: IVolatileSkin;
    }

    export enum JSLocationEnum {
        Top = 0,
        Bottom = 1,
    }
    export enum CssLocationEnum {
        Top = 0,
        Bottom = 1,
    }

    export interface OriginListEntry {
        Url: string;
        EditMode: boolean;
        InPopup: boolean;
    }
    export enum UnifiedModeEnum {
        None = 0,
        HideDivs = 1, // divs for other urls are hidden
        ShowDivs = 2, // all divs are shown
        DynamicContent = 3,
        SkinDynamicContent = 4,
    }

    export interface IVolatileBasics {

        // Site settings
        JSLocation: JSLocationEnum;
        CssLocation: CssLocationEnum;
        CacheVersion: string;

        // User language
        Language: string;

        // Page/Module Edit Control
        OriginList: OriginListEntry[];
        EditModeActive: boolean;
        PageControlVisible: boolean;
        IsInPopup: boolean;

        // classes that don't use tooltips
        CssNoTooltips: string;

        // Page
        PageGuid: string;
        CharWidthAvg: number;
        CharHeight: number;

        // Unified Page Sets
        UnifiedMode: UnifiedModeEnum;
        UnifiedAnimation: number;
        UnifiedSetGuid: string;
        UnifiedPopups: boolean;
        UnifiedSkinCollection: string;
        UnifiedSkinName: string;
        UniqueIdPrefixCounter: number;
        UnifiedCssBundleFiles: string[];
        UnifiedScriptBundleFiles: string[];
        UnifiedAddonModsPrevious: string[];
        UnifiedAddonMods: string[];

        KnownScriptsDynamic: string[];
    }
    export interface IVolatileSkin {

        // Popup information
        PopupWidth: number;
        PopupHeight: number;
        PopupMaximize: boolean;
        PopupCss: string;
        MinWidthForPopups: number;

        // Bootstrap info
        Bootstrap: boolean;
        BootstrapButtons: boolean;
    }

    // CONFIGS
    // CONFIGS
    // CONFIGS

    export interface IConfigs {
        Basics: IConfigsBasics;
    }
    export interface IConfigsBasics {

        Link_OriginList: string;    // chain of urls
        Link_InPopup: string;       // we're in a popup
        Link_ToEditMode: string;    // force this mode
        Link_ToPopup: string;       // we're going into a popup
        Link_PageControl: string;   // show page control module
        Link_CharInfo: string;      // character info (char width, char height) for module issuing req.
        Link_SubmitIsApply: string; // a submit button was clicked and should be handled as Apply
        Link_EditMode: string;      // site edit mode
        Link_NoEditMode: string;    // site display mode
        Link_ScrollLeft: string;
        Link_ScrollTop: string;

        // Css
        CssTooltip: string;
        CssTooltipSpan: string;
        CssLegend: string;
        CssPopupLink: string;
        CssConfirm: string;
        CssPleaseWait: string;
        CssDontAddToOriginList: string;
        CssAddModuleContext: string;
        CssAttrDataSpecialEdit: string;
        CssAttrActionButton: string;
        ModuleGuid: string;

        TemplateName: string;
        TemplateAction: string;
        TemplateExtraData: string;

        AjaxJavascriptErrorReturn: string;
        DefaultPleaseWaitWidth: number;
        DefaultPleaseWaitHeight: number;
        DefaultAlertWaitWidth: number;
        DefaultAlertWaitHeight: number;
        DefaultAlertYesNoWidth: number;
        DefaultAlertYesNoHeight: number;
        DefaultTooltipWidth: number;
        DefaultTooltipPosition: string;

        CookieDoneCssAttr: string;
        CookieDone: string;
        CookieToReturn: string;
        PostAttr: string;
        CssOuterWindow: string;

        CssSaveReturnUrl: string;

        AjaxJavascriptReturn: string;
        AjaxJavascriptReloadPage: string;
        AjaxJavascriptReloadModule: string;
        AjaxJavascriptReloadModuleParts: string;
    }

    // LOCALIZATION
    // LOCALIZATION
    // LOCALIZATION

    export interface ILocs {
        Basics: ILocsBasics;
    }
    export interface ILocsBasics {

        // Button Text
        CloseButtonText: string;
        OKButtonText: string;
        YesButtonText: string;
        NoButtonText: string;

        // Popup Text
        PleaseWaitText: string;
        PleaseWaitTitle: string;
        DefaultAlertYesNoTitle: string;
        DefaultAlertTitle: string;
        DefaultErrorTitle: string;
        DefaultSuccessTitle: string;

        // Links
        OpenNewWindowTT: string;

        // Server response
        IncorrectServerResp: string;
    }
}