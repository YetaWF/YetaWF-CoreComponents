![Yet Another Web Framework](logo.jpg)

![Build status](https://teamcity.softelvdm.com/app/rest/builds/project:YetaWF%20Solution2/statusIcon.svg)

*STOP* - Do not simply retrieve the repository. There [are a few easy steps](https://yetawf.com/Documentation/YetaWF/Topic/g_installing_git) you need to follow to correctly install YetaWF.

# Overview

YetaWF (pronounced "Yet Another Web Framework") is an open source product, developed by [Mike van der Meulen](https://mikevdm.com) ([Softel vdm, Inc.](https://softelvdm.com)).
It was used to develop and host the company's [own site](https://softelvdm.com), its new legal service [Formulaws.com](https://formulaws.com), the [Self-Hosted Docker Registry](https://registryui.com),
the [ubackup.io](https://ubackup.io) service (scheduled, automated SQL, MySQL and PostgreSQL backups), the [www.StatusPie.com](https://www.statuspie.com) Website Uptime Monitoring service, the [LinksWithPics service](https://linkswithpics.com) and various other sites.

It builds on .NET 5.0, MVC to create a web framework for developers.

YetaWF offers [Single Page Sites](https://YetaWF.com/Documentation/YetaWF/Topic/g_doc_unifiedpagesets) "out of the box" (every site is a single page site) and [Static Pages](https://YetaWF.com/Documentation/YetaWF/Topic/g_doc_staticpages). It follows [ROCA principles](https://roca-style.org/) and best practices (like using WEBP for images) so landing pages can easily achieve a [Page Insights](https://developers.google.com/speed/pagespeed/insights/) score in the high 90s, both mobile and desktop).

While it has many attributes commonly found in today's content management systems (CMS), it instead focuses on rapid application development.
It lends itself to rapid module development, offering built-in BREAD (Browse, Read, Edit, Add, Delete) module generation, taking advantage of .NET Core MVC, Data Annotation, Components, UIHint and many other innovative features made possible by .NET Core.
Rather than being all things to all people, it narrowly focuses on offering 100% of the features that will satisfy most average websites and offers a platform for rapid custom development.

It makes extensive use of TypeScript, native JavaScript and many client-side components like grids, menus, syntax highlighter, lightbox, CKEditor, tabs, overlays and many more.
It combines these with automatic features like CDN support, JavaScript and CSS minifier and bundling, HTTP response compression (take a look at the source for [this page](https://YetaWF.com)) without burdening the developer with administrative tasks.

For the site administrator, it offers site management, automated backups, easy version upgrades, scheduled tasks and of course complete control over user/role authorizations.

YetaWF also takes a new approach to localization by being designed from the ground up with localization in mind (again, without burdening the developer with administrative tasks). Pages, modules, down to SQL/PostgreSQL tables or file data are fully localizable.
Adding/removing languages is possible at any time, not just at design-time.

## Environments

- Windows (IIS)
- Linux
- Docker

## Supported Database Servers

- Microsoft SQL (2012+)
- PostgreSQL 11+


## Links

*STOP* - Do not simply retrieve the repository. There are [a few easy steps](https://yetawf.com/Documentation/YetaWF/Topic/g_installing_git) you need to follow to correctly install YetaWF.

* [YetaWF.com](https://yetawf.com)
* [Documentation](https://yetawf.com/Documentation/YetaWF)
* [Installing YetaWF](https://yetawf.com/Documentation/YetaWF/Topic/g_installing)
* [Demo Site](https://demo.YetaWF.com)
* [Using YetaWF](https://yetawf.com/Documentation/YetaWF/Topic/g_doc_gettingstarted)
