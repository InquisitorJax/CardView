using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.ViewPager.Widget;
using PanCardView;
using PanCardView.Droid;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

//[assembly: ExportRenderer(typeof(CardsView), typeof(ViewPagerCardsViewRenderer))]
namespace PanCardView.Droid
{
	[Preserve(AllMembers = true)]

	public class ViewPagerCardsViewRenderer : ViewRenderer<CardsView, Android.Views.View>
	{
		// ref: https://github.com/alexrainman/CarouselView/blob/master/CarouselView/CarouselView.FormsPlugin.Android/CarouselViewImplementation.cs

		private HorizontalViewPager _viewPager;
		private Context _context;
		private Activity _activity;

		// To avoid triggering Position changed more than once
		bool _isChangingPosition;
		bool _isChangingSelectedItem;


		public ViewPagerCardsViewRenderer(Context context) : base(context)
		{
			_context = context;
			_activity = FindActivity(_context);

		}

		private Activity FindActivity(Context context)
		{
			var activity = context as Activity;
			if (activity != null)
			{
				return activity;
			}
			var contextThemeWrapper = _context as Android.Views.ContextThemeWrapper;
			if (contextThemeWrapper != null)
			{
				return FindActivity(contextThemeWrapper.BaseContext);
			}
			return null;
		}

		public static void Preserve()
		=> Preserver.Preserve();

		protected override void OnElementChanged(ElementChangedEventArgs<CardsView> e)
		{
			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Element == null || _viewPager == null) return;

			switch (e.PropertyName)
			{
				case nameof(CardsView.IsVisible):
					_viewPager.Visibility = Element.IsVisible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Invisible;
					break;
				case nameof(CardsView.BackgroundColor):
					_viewPager.SetBackgroundColor(Element.BackgroundColor.ToAndroid());
					break;
				case nameof(CardsView.ItemsSource):
					SetPosition();
					ResetAdapter();
					SetSelectedItemFromIndex();
					break;
				case nameof(CardsView.ItemTemplate):
					ResetAdapter();
					SetSelectedItemFromIndex();
					break;
				case nameof(CardsView.IsPanSwipeEnabled):
					SetIsSwipeEnabled();
					break;
				case nameof(CardsView.SelectedIndex):
					if (!_isChangingPosition)
					{
						SetCurrentPage(Element.SelectedIndex);
					}
					break;
				case nameof(CardsView.SelectedItem):
					if (!_isChangingSelectedItem)
					{
						Element.SelectedIndex = Element.ItemsSource.GetList().IndexOf(Element.SelectedItem);
					}
					break;
			}

		}

		private void SetSelectedItemFromIndex()
		{
			// replaces SendPositionSelected from ref:
			_isChangingSelectedItem = true;
			Element.SelectedItem = Element.ItemsSource?.GetItem(Element.SelectedIndex);
			_isChangingSelectedItem = false;
		}

		private void SetPosition()
		{
			_isChangingPosition = true;
			if (Element.ItemsSource != null)
			{
				if (Element.SelectedIndex > Element.ItemsSource.GetCount() - 1)
				{
					Element.SelectedIndex = Element.ItemsSource.GetCount() - 1;
				}
				if (Element.SelectedIndex == -1)
				{
					Element.SelectedIndex = 0;
				}
			}
			else
			{
				Element.SelectedIndex = 0;
			}
			_isChangingPosition = false;
		}

		void SetNativeView()
		{
			_viewPager = new HorizontalViewPager(_context);

			_viewPager.Adapter = new PageAdapter(Element, _activity);

			// NEW: set current item to +1 if infinite scrolling
			var currentItem = Element.SelectedIndex;

			_viewPager.SetCurrentItem(currentItem, false);

			// InterPageSpacing BP
			//var metrics = Resources.DisplayMetrics;
			//var interPageSpacing = Element.InterPageSpacing * metrics.Density;
			_viewPager.PageMargin = 8;

			// BackgroundColor BP
			_viewPager.SetBackgroundColor(Element.BackgroundColor.ToAndroid());

			_viewPager.PageSelected += ViewPager_PageSelected;

			// IsSwipeEnabled BP
			SetIsSwipeEnabled();

			// TapGestureRecognizer doesn't work when added to CarouselViewControl (Android) #66, #191, #200
			_viewPager?.SetElement(Element);

			SetNativeControl(_viewPager);
		}

		private void SetCurrentPage(int position)
		{
			if (position < 0 || position > Element.ItemsSource?.GetCount() - 1) return;

			if (Element == null || _viewPager == null || Element.ItemsSource == null) return;

			if (Element.ItemsSource?.GetCount() > 0)
			{
				_viewPager.SetCurrentItem(position, smoothScroll: true);
				SetSelectedItemFromIndex();
			}
		}

		private void SetIsSwipeEnabled()
		{
			_viewPager?.SetPagingEnabled(Element.IsPanSwipeEnabled);
		}

		private void ResetAdapter()
		{
			_viewPager.Adapter = new PageAdapter(Element, _activity);
		}


		#region adapter callbacks

		// To assign position when page selected
		private void ViewPager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
		{
			// To avoid calling SetCurrentPage
			_isChangingPosition = true;
			Element.SelectedIndex = e.Position;
			_isChangingPosition = false;
		}

		#endregion

		private class PageAdapter : PagerAdapter
		{
			CardsView _element;
			Context _context;

			// A local copy of ItemsSource so we can use CollectionChanged events
			public List<object> Source;

			public PageAdapter(CardsView element, Context context)
			{
				_element = element;
				this._context = context;

				Source = _element.ItemsSource != null ? new List<object>(_element.ItemsSource.GetList()) : null;
			}

			public override int Count
			{
				get
				{
					return Source?.Count ?? 0;
				}
			}

			public override bool IsViewFromObject(Android.Views.View view, Java.Lang.Object @object)
			{
				return view == @object;
			}

			public override Java.Lang.Object InstantiateItem(Android.Views.ViewGroup container, int position)
			{
				object bindingContext = null;

				if (Source != null && Source?.Count > 0)
				{
					bindingContext = Source.Cast<object>().ElementAt(position);
				}

				var view = bindingContext as View;

				// Support for List<DataTemplate> as ItemsSource
				var dt = bindingContext as DataTemplate;

				View formsView;
				if (dt != null)
				{
					formsView = (View)dt.CreateContent();
				}
				else
				{
					if (view != null)
					{
						formsView = view;
					}
					else
					{
						var selector = _element.ItemTemplate as DataTemplateSelector;
						if (selector != null)
						{
							formsView = (View)selector.SelectTemplate(bindingContext, _element).CreateContent();
						}
						else
						{
							// So ItemsSource can be ViewModels
							if (_element.ItemTemplate != null)
							{
								formsView = (View)_element.ItemTemplate.CreateContent();
							}
							else
							{
								formsView = new Label()
								{
									Text = "Please provide an ItemTemplate or a DataTemplateSelector"
								};
							}
						}

						formsView.BindingContext = bindingContext;
					}
				}

				// HeightRequest fix
				formsView.Parent = this._element;

				var size = new Rectangle(0, 0, _element.Width, _element.Height);

				var nativeConverted = formsView.ToAndroid(size, _context);
				nativeConverted.Tag = new Tag() { BindingContext = bindingContext }; //position;

				var pager = (ViewPager)container;
				pager.AddView(nativeConverted);

				return nativeConverted;
			}

			public override void DestroyItem(Android.Views.ViewGroup container, int position, Java.Lang.Object @object)
			{
				var pager = (ViewPager)container;
				var view = (Android.Views.View)@object;
				pager.RemoveView(view);
				//[Android] Out of memories(FFImageLoading + CarouselView) #279
				view.UnbindDrawables();
				view.Dispose();
			}

			public override int GetItemPosition(Java.Lang.Object @object)
			{
				var tag = (Tag)((Android.Views.View)@object).Tag;
				var position = Source.IndexOf(tag.BindingContext);
				return position != -1 ? position : PositionNone;
			}


		}
	}

	internal sealed class Tag : Java.Lang.Object
	{
		public object BindingContext { get; set; }
	}

}