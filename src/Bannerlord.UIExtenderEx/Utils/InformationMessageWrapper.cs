﻿namespace Bannerlord.UIExtenderEx.Utils
{
    internal sealed record InformationMessageWrapper(object Object)
    {
        public static InformationMessageWrapper Create(object @object) => new(@object);
    }
}