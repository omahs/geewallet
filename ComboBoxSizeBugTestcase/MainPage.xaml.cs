﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ComboBoxSizeBugTestcase
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            Device.BeginInvokeOnMainThread(() =>
                {

                    this.currencySelector.Items.Add("CORP");
                    this.currencySelector.Items.Add("LTD");
                    this.currencySelector.SelectedIndex = 1;
                }
            );
        }
    }
}
