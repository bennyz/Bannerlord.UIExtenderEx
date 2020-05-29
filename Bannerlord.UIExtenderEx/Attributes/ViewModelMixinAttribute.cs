﻿namespace Bannerlord.UIExtenderEx.Attributes
{
    /// <summary>
    /// Attribute to mark view model mixins.
    /// Mixin classes should extend from `BaseViewModelMixin<T>` and should be marked with this attribute
    /// </summary>
    public sealed class ViewModelMixinAttribute : BaseUIExtenderAttribute
    {
        public string? RefreshMethodName { get; }

        public ViewModelMixinAttribute() { }
        public ViewModelMixinAttribute(string refreshMethodName)
        {
            RefreshMethodName = refreshMethodName;
        }
    }
}