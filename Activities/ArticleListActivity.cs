using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using static Android.Widget.AdapterView;

namespace VorratsUebersicht
{
    [Activity(Label = "@string/Main_Button_ArtikelListe", Icon = "@drawable/ic_local_offer_white_48dp")]
    public class ArticleListActivity : Activity, SearchView.IOnQueryTextListener
    {
        List<ArticleListView> liste = new List<ArticleListView>();
        private IParcelable listViewState;

		private bool selectArticleOnly;
        private string category;
        private string subCategory;
        private string lastSearchText = string.Empty;
        private List<string> categoryList;
        Toast toast;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.selectArticleOnly = Intent.GetBooleanExtra("SelectArticleOnly", false);

            this.category    = Intent.GetStringExtra ("Category") ?? string.Empty;
            this.subCategory = Intent.GetStringExtra ("SubCategory") ?? string.Empty;

            if (!string.IsNullOrEmpty(this.subCategory))
            {
                this.Title = string.Format("{0} - {1}", this.Title, this.subCategory);
            }

            SetContentView(Resource.Layout.ArticleList);

            // ActionBar Hintergrund Farbe setzen
            var backgroundPaint = ContextCompat.GetDrawable(this, Resource.Color.Application_ActionBar_Background);
            backgroundPaint.SetBounds(0, 0, 10, 10);
            ActionBar.SetBackgroundDrawable(backgroundPaint);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            this.categoryList = new List<string>();
            this.categoryList.Add(Resources.GetString(Resource.String.ArticleList_AllCategories));
            this.categoryList.AddRange(Database.GetCategoryAndSubCategoryNames());

            if (categoryList.Count > 1)
            {
                var categorySelection = FindViewById<LinearLayout>(Resource.Id.ArticleList_SelectCategory);
                categorySelection.Visibility = ViewStates.Visible;

                var spinnerCategory = FindViewById<Spinner>(Resource.Id.ArticleList_Categories);
                ArrayAdapter<String> dataAdapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleSpinnerItem, this.categoryList);
                dataAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                spinnerCategory.Adapter = dataAdapter;

                spinnerCategory.ItemSelected += SpinnerCategory_ItemSelected;
            }

            ListView listView = FindViewById<ListView>(Resource.Id.ArticleList);
            listView.ItemClick += OnOpenArticleDetails;

            this.RegisterForContextMenu(listView);

            ShowArticleList();
        }

        public override void OnCreateContextMenu(IContextMenu menu, View view, IContextMenuContextMenuInfo menuInfo)
        {
            if (view.Id == Resource.Id.ArticleList) 
            {
                ListView listView = (ListView)view;
                AdapterView.AdapterContextMenuInfo acmi = (AdapterContextMenuInfo) menuInfo;
                //ArticleListView obj = (ArticleListView)listView.GetItemAtPosition(acmi.Position);

                menu.Add(Menu.None, 1, Menu.None, Resource.String.ArticleList_ToShoppingList);  // Auf Einkaufszettel
                menu.Add(Menu.None, 2, Menu.None, Resource.String.ArticleList_Lagerbestand);    // Lagerbestand

            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            AdapterContextMenuInfo info = (AdapterContextMenuInfo) item.MenuInfo;

            ListView listView = FindViewById<ListView>(Resource.Id.ArticleList);
            ArticleListView  selectedItem = Tools.Cast<ArticleListView>(listView.Adapter.GetItem(info.Position));

            switch(item.ItemId)
            {
                case 1: // Auf Einkaufszettel
                    this.AddToShoppingListAutomatically(selectedItem.Id);
                    return true;

                case 2: // Lagerbestand
                    var storageDetails = new Intent(this, typeof(StorageItemQuantityActivity));
                    storageDetails.PutExtra("ArticleId", selectedItem.Id);

                    this.StartActivity(storageDetails);
                    return true;

                default:
                    return base.OnContextItemSelected(item);
            }
        }

        private void SpinnerCategory_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string newCategoryName    = string.Empty;
            string newSubCategoryName = string.Empty;

            string name = this.categoryList[e.Position];

            if (e.Position > 0)
            {
                if (name.StartsWith("  - "))    // Ist das ein SubCategory?
                {
                    name = name.Substring(4);  // Mach aus "  - Gulasch" ein "Gulasch"
                    newSubCategoryName = name;
                }
                else
                {
                    newCategoryName = name;
                }
            }

            if ((newCategoryName != this.category) || (newSubCategoryName != this.subCategory))
            {
                this.category    = newCategoryName;;
                this.subCategory = newSubCategoryName;

                this.ShowArticleList(this.lastSearchText);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.ArticleList_menu, menu);

            var searchMenuItem = menu.FindItem(Resource.Id.ArticleList_Search);
            var searchView = (SearchView) searchMenuItem.ActionView;

            // https://coderwall.com/p/zpwrsg/add-search-function-to-list-view-in-android
            searchView.SetOnQueryTextListener(this);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    this.OnBackPressed();
                    return true;

                case Resource.Id.ArticleList_Add:
                    // Create New Article
                    this.ShowArticleDetails(0, null);

                    return true;
            }
            return true;
        }

        private void ShowArticleList(string text = null)
        {
            this.liste = new List<ArticleListView>();

            var articleList = Database.GetArticleListNoImages(this.category, this.subCategory, text);

            foreach(Article article in articleList)
            {
                liste.Add(new ArticleListView(article));
            }

            ArticleListViewAdapter listAdapter = new ArticleListViewAdapter(this, liste);

            ListView listView = FindViewById<ListView>(Resource.Id.ArticleList);
            listView.Adapter = listAdapter;

            string status;

            if (articleList.Count == 1)
                status = string.Format("{0:n0} Position", articleList.Count);
            else
                status = string.Format("{0:n0} Positionen", articleList.Count);

            TextView footer = FindViewById<TextView>(Resource.Id.ArticleList_Footer);
            footer.Text = status;
        }

        private void OnOpenArticleDetails(object sender, AdapterView.ItemClickEventArgs e)
        {
            Java.Lang.Object itemObject = ((ListView)sender).GetItemAtPosition(e.Position);

            ArticleListView item = Tools.Cast<ArticleListView>(itemObject);

			// Nur Artikelauswahl, keine Detailsbearbeitung
			if (this.selectArticleOnly)
			{
                Intent intent = new Intent();
				intent.PutExtra("Heading",   item.Heading);
				intent.PutExtra("ArticleId", item.Id);

                this.SetResult(Result.Ok, intent);

                this.OnBackPressed();
				return;
			}

            this.ShowArticleDetails(item.Id, item.Heading);
        }

        private void ShowArticleDetails(int articleId, string name)
        {
            var articleDetails = new Intent (this, typeof(ArticleDetailsActivity));
            articleDetails.PutExtra("Name", name);
            articleDetails.PutExtra("ArticleId", articleId);

            // Zum voranstellen
            articleDetails.PutExtra("Category",    this.category);
            articleDetails.PutExtra("SubCategory", this.subCategory);

            StartActivityForResult(articleDetails, 10);

            ListView listView = FindViewById<ListView>(Resource.Id.ArticleList);
            this.listViewState = listView.OnSaveInstanceState();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (resultCode == Result.Ok)
            {
                this.ShowArticleList(this.lastSearchText);

                if (this.listViewState == null)
                    return;
                    
                ListView listView = FindViewById<ListView>(Resource.Id.ArticleList);
                listView?.OnRestoreInstanceState(this.listViewState);
            }
        }

        public bool OnQueryTextChange(string filter)
        {
            if (this.lastSearchText == filter)
                return true;

            // Filter ggf. mit Adapter, siehe https://coderwall.com/p/zpwrsg/add-search-function-to-list-view-in-android
            ShowArticleList(filter);
            this.lastSearchText = filter;
            return true;
        }

        public bool OnQueryTextSubmit(string filter)
        {
            if (this.lastSearchText == filter)
                return true;

            // Filter ggf. mit Adapter, siehe https://coderwall.com/p/zpwrsg/add-search-function-to-list-view-in-android
            ShowArticleList(filter);
            this.lastSearchText = filter;
            return true;
        }

        private void AddToShoppingListAutomatically(int articleId)
        {
            int toBuyQuantity = Database.GetToShoppingListQuantity(articleId);
            if (toBuyQuantity == 0)
                toBuyQuantity = 1;

            double count = Database.AddToShoppingList(articleId, toBuyQuantity);

            string msg = string.Format("{0} St�ck auf der Einkaufsliste.", count);
            if (this.toast != null)
            {
                this.toast.Cancel();
                this.toast = Toast.MakeText(this, msg, ToastLength.Short);
            }
            else
            {
                this.toast = Toast.MakeText(this, msg, ToastLength.Short);
            }

            this.toast.Show();
        }

    }
}