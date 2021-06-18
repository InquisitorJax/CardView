using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.ViewPager.Widget;
using PanCardView.Enums;
using System;
using System.Linq;
using Xamarin.Forms;

namespace PanCardView.Droid
{
	public sealed class HorizontalViewPager : ViewPager
    {
        private bool _isSwipingEnabled = true;
        private CardsView _element;

        public HorizontalViewPager(Context context) : base(context, null)
        {
        }

        public HorizontalViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        // Fix for #171 System.MissingMethodException: No constructor found
        public HorizontalViewPager(IntPtr intPtr, JniHandleOwnership jni) : base(intPtr, jni)
        {
        }

        float mStartDragX;
        float mStartDragY;

        public override bool OnInterceptTouchEvent(MotionEvent e)
        {
            if (this._isSwipingEnabled)
            {
                if (e.Action == MotionEventActions.Down)
                {
                    mStartDragX = e.GetX();
                }

                if (_element.GestureRecognizers.FirstOrDefault((arg) => arg is SwipeGestureRecognizer) is SwipeGestureRecognizer swipe)
                {
                    mStartDragY = e.GetY();
                    return true;
                }

                return base.OnInterceptTouchEvent(e);
            }

            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {

            if (e.Action == MotionEventActions.Up && _element?.GestureRecognizers.GetCount() > 0)
            {
                if (_element.GestureRecognizers.FirstOrDefault((arg) => arg is TapGestureRecognizer) is TapGestureRecognizer tap)
                {
                    tap.Command?.Execute(tap.CommandParameter);
                }
            }

            if (this._isSwipingEnabled)
            {
                if (e.Action == MotionEventActions.Up)
                {
                    string swipeDirection = "";

                    var CumulativeX = e.GetX() - mStartDragX;
                    var CumulativeY = e.GetY() - mStartDragY;

                    if (Math.Abs(CumulativeX) < Math.Abs(CumulativeY))
                    {
                        if (CumulativeY > 0)
                        {
                            swipeDirection = ItemSwipeDirection.Down.ToString();
                        }
                        else
                        {
                            swipeDirection = ItemSwipeDirection.Up.ToString();
                        }
                    }

                    if (_element.GestureRecognizers.FirstOrDefault((arg) => arg is SwipeGestureRecognizer) is SwipeGestureRecognizer swipe && mStartDragY > 0)
                    {
                        if (swipe.Direction.ToString().Contains(swipeDirection))
                        {
                            swipe.Command?.Execute(swipe.CommandParameter);
                        }
                    }

                    float x = e.GetX();
                }

                return base.OnTouchEvent(e);
            }

            return false;
        }

        public void SetElement(CardsView element)
        {
            _element = element;
        }

        public void SetPagingEnabled(bool enabled)
        {
            _isSwipingEnabled = enabled;
        }
    }
}