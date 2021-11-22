﻿//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using AsyncAwaitBestPractices;
//using Microsoft.Maui.Controls;
//using Microsoft.Maui.Essentials;


//namespace rzrboy.ViewModels
//{
//	abstract class BaseViewModel : INotifyPropertyChanged
//	{
//		readonly WeakEventManager _propertyChangedEventManager = new();

//		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
//		{
//			add => _propertyChangedEventManager.AddEventHandler(value);
//			remove => _propertyChangedEventManager.RemoveEventHandler(value);
//		}

//		protected void SetProperty<T>(ref T backingStore, in T value, in System.Action? onChanged = null, [CallerMemberName] in string propertyname = "")
//		{
//			if (EqualityComparer<T>.Default.Equals(backingStore, value))
//				return;

//			backingStore = value;

//			onChanged?.Invoke();

//			OnPropertyChanged(propertyname);
//		}

//		protected void OnPropertyChanged([CallerMemberName] in string propertyName = "") =>
//			_propertyChangedEventManager.RaiseEvent(this, new PropertyChangedEventArgs(propertyName), nameof(INotifyPropertyChanged.PropertyChanged));
//	}
//}