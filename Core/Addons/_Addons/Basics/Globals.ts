/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
    export enum MessageTypeEnum {
        Popups = 0,
        ToastRight = 10,
        ToastLeft = 11,
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
        TemporaryPage: boolean;

        // Unified Page Sets
        UniqueIdCounters: UniqueIdInfo;
        UnifiedCssBundleFiles: string[];
        UnifiedScriptBundleFiles: string[];
        UnifiedAddonModsPrevious: string[];
        UnifiedAddonMods: string[];

        // Popups
        ForcePopup: boolean;

        KnownScriptsDynamic: string[];
    }
    export interface UniqueIdInfo {
        UniqueIdPrefix: string;
        UniqueIdPrefixCounter: number;
        UniqueIdCounter: number;
    }
    export interface IVolatileSkin {

        // Popup information
        PopupWidth: number;
        PopupHeight: number;
        PopupMaximize: boolean;
        PopupCss: string;
        MinWidthForPopups: number;
        MinWidthForCondense: number;
    }

    // CONFIGS
    // CONFIGS
    // CONFIGS

    export interface IConfigs {
        Basics: IConfigsBasics;
        SignalR: IConfigsSignalR;
    }
    export interface IConfigsBasics {

        DEBUGBUILD: boolean;

        ApiPrefix: string;          // API Prefix, internal API
        Link_OriginList: string;    // chain of urls
        Link_InPopup: string;       // we're in a popup
        Link_ToPopup: string;       // we're going into a popup
        Link_PageControl: string;   // show page control module
        Link_SubmitIsApply: string; // a submit button was clicked and should be handled as Apply
        Link_SubmitIsReload: string; // a submit button was clicked and should be handled as a form reload
        Link_EditMode: string;      // site edit mode
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
        CssAttrDataSpecialEdit: string;
        CssAttrActionButton: string;
        ModuleGuid: string;

        TemplateName: string;
        TemplateAction: string;
        TemplateExtraData: string;

        AjaxJavascriptErrorReturn: string;
        MessageType: number;
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
        AjaxJSONReturn: string;
        AjaxJavascriptReloadPage: string;
        AjaxJavascriptReloadModule: string;
        AjaxJavascriptReloadModuleParts: string;
    }
    export interface IConfigsSignalR {
        Url: string;
        Version: string;
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

    // PARTIAL VIEW
    // PARTIAL VIEW
    // PARTIAL VIEW

    export interface PartialViewData {
        __UniqueIdCounters: YetaWF.UniqueIdInfo,
        __ModuleGuid: string|null; // The module for which the partial view is rendered
        __RequestVerificationToken: string;
    }

    // DATAPROVIDER
    // DATAPROVIDER
    // DATAPROVIDER

    export enum SortDirection {
        Asending = 0,
        Descending = 1,
    }

    export interface DataProviderSortInfo {
        Field: string;
        Order: SortDirection;
    }
    export interface DataProviderFilterInfo {
        Field: string;
        Operator: string;
        ValueAsString: string;
    }

}