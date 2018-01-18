/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Support {
    public static class Gravatar {
        public enum GravatarEnum {
            [EnumDescription("(none)", "No image is shown")]
            None = 0,
            [EnumDescription("Mystery Man", "A simple, cartoon-style silhouetted outline of a person - same for all email addresses")]
            mm = 1,
            [EnumDescription("Identicon", "A geometric pattern - varies by email address")]
            identicon = 2,
            [EnumDescription("Monsterid", "A generated 'monster' with different colors, faces, etc. - varies by email address")]
            monsterid = 3,
            [EnumDescription("Wavatar", "A generated face with differing features and backgrounds - varies by email address")]
            wavatar = 4,
            [EnumDescription("Retro", "An awesome, generated 8-bit arcade-style pixelated face - varies by email address")]
            retro = 5,
            [EnumDescription("Blank", "A transparent PNG image - same for all email addresses")]
            blank = 6,
        }
        public enum GravatarRatingEnum {
            [EnumDescription("General", "Gravatars suitable for display on all websites with any audience type")]
            G = 0,
            [EnumDescription("Mildly Inappropriate", "Gravatars may contain rude gestures, provocatively dressed individuals, the lesser swear words, or mild violence")]
            PG = 1,
            [EnumDescription("Inappropriate", "Gravatars may contain such things as harsh profanity, intense violence, nudity, or hard drug use")]
            R = 2,
            [EnumDescription("Seriously Inappropriate", "Gravatars may contain hardcore sexual imagery or extremely disturbing violence")]
            X = 3,
        }
    }
}
