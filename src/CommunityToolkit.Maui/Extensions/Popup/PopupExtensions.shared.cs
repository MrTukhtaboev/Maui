﻿using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
	/// <summary>
	/// Displays a popup.
	/// </summary>
	/// <param name="page">
	/// The current <see cref="Page"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="BasePopup"/> to display.
	/// </param>
	public static void ShowPopup<TPopup>(this Page page, TPopup popup) where TPopup : BasePopup
	{
#if WINDOWS
		PlatformShowPopup(popup, GetMauiContext(page));
#else
		CreatePopup(page, popup);
#endif

	}

	/// <summary>
	/// Displays a popup and returns a result.
	/// </summary>
	/// <param name="page">
	/// The current <see cref="Page"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="Popup"/> to display.
	/// </param>
	/// <returns>
	/// A task that will complete once the <see cref="Popup"/> is dismissed.
	/// </returns>
	public static Task<object?> ShowPopupAsync<TPopup>(this Page page, TPopup popup) where TPopup : Popup
	{
#if WINDOWS
		return PlatformShowPopupAsync(popup, GetMauiContext(page));
#else

		CreatePopup(page, popup);
		return popup.Result;
#endif
	}

	static void CreatePopup(Page page, BasePopup popup)
	{
		var mauiContext = GetMauiContext(page);
		var popupNative = popup.ToHandler(mauiContext);
		popupNative.Invoke(nameof(IPopup.OnOpened));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static IMauiContext GetMauiContext(Page page)
	{
		return page.Handler?.MauiContext ?? throw new InvalidOperationException("Could locate MauiContext");
	}
}

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.BasePopup");

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.Popup.");
}
#endif