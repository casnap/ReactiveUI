using System;
using ReactiveUI;
using System.Linq;
using System.Reactive.Linq;

#if UIKIT
using MonoTouch.UIKit;

using NSView = MonoTouch.UIKit.UIView;
using NSViewController = MonoTouch.UIKit.UIViewController;
#else
using MonoMac.AppKit;
#endif

namespace ReactiveUI.Cocoa
{
    public class RoutedViewHost : ReactiveObject
    {
        IRoutingState _Router;
        public IRoutingState Router {
            get { return _Router; }
            set { this.RaiseAndSetIfChanged(ref _Router, value); }
        }

        IObservable<string> _ViewContractObservable;
        public IObservable<string> ViewContractObservable {
            get { return _ViewContractObservable; }
            set { this.RaiseAndSetIfChanged(ref _ViewContractObservable, value); }
        }
        
        NSViewController _DefaultContent;
        public NSViewController DefaultContent {
            get { return _DefaultContent; }
            set { this.RaiseAndSetIfChanged(ref _DefaultContent, value); }
        }
        
        public IViewLocator ViewLocator { get; set; }

        public RoutedViewHost(NSView targetView)
        {
            NSView viewLastAdded = null;
                        
            var vmAndContract = Observable.CombineLatest(
                this.WhenAnyObservable(x => x.Router.CurrentViewModel),
                this.WhenAnyObservable(x => x.ViewContractObservable),
                (vm, contract) => new { ViewModel = vm, Contract = contract, });

            vmAndContract.Subscribe(x => {
                if (viewLastAdded != null) viewLastAdded.RemoveFromSuperview();

                if (x.ViewModel == null) {
                    if (DefaultContent != null) targetView.AddSubview(DefaultContent.View);
                    return;
                }

                var viewLocator = ViewLocator ?? ReactiveUI.ViewLocator.Current;
                var view = viewLocator.ResolveView(x.ViewModel, x.Contract);
                view.ViewModel = x.ViewModel;

                if (view is NSViewController) {
                    viewLastAdded = ((NSViewController)view).View;
                } else if (view is NSView) { 
                    viewLastAdded = (NSView)view;
                } else {
                    throw new Exception(String.Format("'{0}' must be an NSViewController or NSView", view.GetType().FullName));
                }

                targetView.AddSubview(viewLastAdded);           
            }, ex => RxApp.DefaultExceptionHandler.OnNext(ex));
        }
    }
}