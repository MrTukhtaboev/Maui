﻿using System;
using CommunityToolkit.Maui.Helpers;
using CommunityToolkit.Maui.UI.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CommunityToolkit.Maui.Platform;

public class PopupRenderer : UIViewController
{
	bool isDisposed;
	readonly IMauiContext mauiContext;

	public PageHandler? Control { get; private set; }

	public IBasePopup? Element { get; private set; }

	public UIView? NativeView => View;

	public UIViewController? ViewController { get; private set; }

	//	[Preserve(Conditional = true)]
	public PopupRenderer(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext;
	}

	public void SetElementSize(Size size) =>
		Control?.ContainerView?.SizeThatFits(size);

	public override void ViewDidLayoutSubviews()
	{
		base.ViewDidLayoutSubviews();

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null");
		SetElementSize(new Size(View.Bounds.Width, View.Bounds.Height));
	}

	public override void ViewDidAppear(bool animated)
	{
		base.ViewDidAppear(animated);

		_ = Element ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		ModalInPopover = !Element.IsLightDismissEnabled;
	}

	//public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint) =>
	//	NativeView.GetSizeRequest(widthConstraint, heightConstraint);

	public void SetElement(IBasePopup element)
	{
		if (element is not IBasePopup)
			throw new ArgumentNullException("Element is not of type " + typeof(BasePopup), nameof(element));

		Element = element;
		CreateControl();
		SetViewController();
		SetPresentationController();
		SetEvents();
		SetView();
		AddToCurrentPageViewController();
	}

	protected virtual void OnElementChanged(ElementChangedEventArgs<BasePopup?> e)
	{
		if (e.NewElement != null && !isDisposed && Element is not null)
		{
			ModalInPopover = true;
			ModalPresentationStyle = UIModalPresentationStyle.Popover;

			//SetViewController();
			//SetPresentationController();
			//SetEvents();
			this.SetSize(Element);
			this.SetLayout(Element);
			this.SetBackgroundColor(Element);
			//SetView();
			//AddToCurrentPageViewController();
		}
	}

	void CreateControl()
	{
		_ = Element ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null.");

		var view = (View?)Element.Content;
		var contentPage = new ContentPage { Content = view };

		Control = (PageHandler)contentPage.ToHandler(mauiContext);

		contentPage.Parent = Application.Current?.MainPage;
		contentPage.SetBinding(VisualElement.BindingContextProperty, new Binding { Source = Element, Path = VisualElement.BindingContextProperty.PropertyName });
	}

	void SetViewController()
	{
		Page currentPageRenderer;
		var modalStackCount = Application.Current?.MainPage?.Navigation?.ModalStack?.Count ?? 0;
		var mainPage = Application.Current?.MainPage ?? throw new NullReferenceException(nameof(Application.Current.MainPage));
		if (modalStackCount > 0)
		{
			var index = modalStackCount - 1;
			currentPageRenderer = mainPage.Navigation.ModalStack[index];
		}
		else
			currentPageRenderer = mainPage;

		var viewController = (currentPageRenderer.Handler.NativeView as PageHandler)?.ViewController;

		ViewController = viewController;
	}

	void SetEvents()
	{
		_ = Element ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");

		if (Element is BasePopup basePopup)
			basePopup.Dismissed += OnDismissed;
	}

	void SetView()
	{
		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null");
		_ = Control ?? throw new InvalidOperationException($"{nameof(Control)} cannot be null");

		View.AddSubview(Control.ViewController?.View ?? throw new NullReferenceException());
		View.Bounds = new(0, 0, PreferredContentSize.Width, PreferredContentSize.Height);
		AddChildViewController(Control.ViewController);
	}

	void SetPresentationController()
	{
		var popOverDelegate = new PopoverDelegate();
		popOverDelegate.PopoverDismissed += HandlePopoverDelegateDismissed;

		((UIPopoverPresentationController)PresentationController).SourceView = ViewController?.View ?? throw new NullReferenceException();

		((UIPopoverPresentationController)PresentationController).Delegate = popOverDelegate;
	}

	void HandlePopoverDelegateDismissed(object? sender, UIPresentationController e)
	{
		_ = Element ?? throw new NullReferenceException();

		if (IsViewLoaded && Element.IsLightDismissEnabled)
			Element.LightDismiss();
	}

	void AddToCurrentPageViewController()
	{
		_ = ViewController ?? throw new InvalidOperationException($"{nameof(ViewController)} cannot be null");
		_ = Element ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");

		ViewController.PresentViewController(this, true, () => Element.OnOpened());
	}

	async void OnDismissed(object? sender, PopupDismissedEventArgs e)
	{
		if (ViewController != null)
			await ViewController.DismissViewControllerAsync(true);
	}

	protected override void Dispose(bool disposing)
	{
		if (isDisposed)
			return;

		isDisposed = true;
		if (disposing)
		{
			if (Element != null)
			{
				Element = null;

				var presentationController = (UIPopoverPresentationController)PresentationController;
				if (presentationController != null)
					presentationController.Delegate = null;
			}
		}

		base.Dispose(disposing);
	}

	class PopoverDelegate : UIPopoverPresentationControllerDelegate
	{
		readonly WeakEventManager<UIPresentationController> popoverDismissedEventManager = new();

		public event EventHandler<UIPresentationController> PopoverDismissed
		{
			add => popoverDismissedEventManager.AddEventHandler(value);
			remove => popoverDismissedEventManager.RemoveEventHandler(value);
		}

		public override UIModalPresentationStyle GetAdaptivePresentationStyle(UIPresentationController forPresentationController) =>
			UIModalPresentationStyle.None;

		public override void DidDismiss(UIPresentationController presentationController) =>
			popoverDismissedEventManager.RaiseEvent(this, presentationController, nameof(PopoverDismissed));
	}
}