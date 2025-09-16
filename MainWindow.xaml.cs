using DictionaryApp.Data;
using DictionaryApp.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DictionaryApp
{
    public partial class MainWindow : Window
    {
        private List<Dict> dicts = new();
        private List<DictItem> items = new();
        private Dict? selectedDict = null;
        private DictItem? selectedItem = null;

        public MainWindow()
        {
            InitializeComponent();
            // Створення БД та початкових таблиць
            Database.Initialize();
            LoadDicts();
        }

        // Завантажити всі словники
        private void LoadDicts()
        {
            dicts = Database.GetAllDicts();
            DictsGrid.ItemsSource = null;
            DictsGrid.ItemsSource = dicts;
            ClearDictEditFields();
            ClearItems();
        }

        // Завантажити елементи для вибраного словника
        private void LoadItemsForSelectedDict()
        {
            if (selectedDict == null)
            {
                ClearItems();
                return;
            }
            items = Database.GetItemsByDict(selectedDict.DictId);
            ItemsGrid.ItemsSource = null;
            ItemsGrid.ItemsSource = items;
            ClearItemEditFields();
        }

        // Очистити поля редагування словника
        private void ClearDictEditFields()
        {
            TxtDictName.Text = "";
            TxtDictCode.Text = "";
            TxtDictDesc.Text = "";
            TxtDictParentId.Text = "";
            selectedDict = null;
        }

        // Очистити поля редагування елемента
        private void ClearItemEditFields()
        {
            TxtItemCode.Text = "";
            TxtItemName.Text = "";
            selectedItem = null;
        }

        // Очистити список елементів
        private void ClearItems()
        {
            items = new List<DictItem>();
            ItemsGrid.ItemsSource = null;
        }

        // Перевірка циклів у ієрархії словників
        private bool IsCyclic(int dictId, int potentialParentId)
        {
            int? currentParent = potentialParentId;
            while (currentParent.HasValue)
            {
                if (currentParent.Value == dictId)
                    return true; // знайдено цикл

                var parent = dicts.FirstOrDefault(d => d.DictId == currentParent.Value);
                currentParent = parent?.ParentId;
            }
            return false;
        }

        // Додати новий словник
        private void BtnAddDict_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtDictName.Text.Trim();
            string code = TxtDictCode.Text.Trim();
            string desc = TxtDictDesc.Text.Trim();
            string parentText = TxtDictParentId.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Назва і код словника обов'язкові.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Перевірка ParentId
            int? parentId = null;
            if (!string.IsNullOrEmpty(parentText))
            {
                if (int.TryParse(parentText, out int pid))
                {
                    if (dicts.Any(d => d.DictId == pid))
                        parentId = pid;
                    else
                    {
                        MessageBox.Show("ParentId не знайдено.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("ParentId має бути числом.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Перевірка на цикл
            if (parentId.HasValue && IsCyclic(0, parentId.Value))
            {
                MessageBox.Show("Операція створить цикл у ієрархії. Спробуйте ввести інший ParentId.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var d = new Dict { Name = name, Code = code, Description = desc, ParentId = parentId };
            int newId = Database.CreateDict(d);
            LoadDicts();

            // Вибрати новий словник у списку
            selectedDict = dicts.FirstOrDefault(x => x.DictId == newId);
            if (selectedDict != null)
            {
                DictsGrid.SelectedItem = selectedDict;
                DictsGrid.ScrollIntoView(selectedDict);
            }
        }

        // Оновити вибраний словник
        private void BtnUpdateDict_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDict == null)
            {
                MessageBox.Show("Оберіть словник для оновлення.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = TxtDictName.Text.Trim();
            string code = TxtDictCode.Text.Trim();
            string desc = TxtDictDesc.Text.Trim();
            string parentText = TxtDictParentId.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Назва і код словника обов'язкові.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Перевірка ParentId
            int? parentId = null;
            if (!string.IsNullOrEmpty(parentText))
            {
                if (int.TryParse(parentText, out int pid))
                {
                    if (dicts.Any(d => d.DictId == pid))
                        parentId = pid;
                    else
                    {
                        MessageBox.Show("ParentId не знайдено.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("ParentId має бути числом.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (parentId.HasValue && IsCyclic(selectedDict.DictId, parentId.Value))
            {
                MessageBox.Show("Операція створить цикл у ієрархії. Спробуйте ввести інший ParentId.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Оновлення даних у БД
            selectedDict.Name = name;
            selectedDict.Code = code;
            selectedDict.Description = desc;
            selectedDict.ParentId = parentId;
            Database.UpdateDict(selectedDict);
            LoadDicts();
        }

        // Видалити вибраний словник
        private void BtnDeleteDict_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDict == null)
            {
                MessageBox.Show("Оберіть словник для видалення.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var res = MessageBox.Show($"Видалити словник \"{selectedDict.Name}\" та всі його елементи?",
                "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                Database.DeleteDict(selectedDict.DictId);
                LoadDicts();
            }
        }

        // Зміна вибраного словника в таблиці
        private void DictsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDict = DictsGrid.SelectedItem as Dict;
            if (selectedDict != null)
            {
                TxtDictName.Text = selectedDict.Name;
                TxtDictCode.Text = selectedDict.Code;
                TxtDictDesc.Text = selectedDict.Description ?? "";
                TxtDictParentId.Text = selectedDict.ParentId?.ToString() ?? "";
            }
            else
            {
                ClearDictEditFields();
            }
            LoadItemsForSelectedDict();
        }

        // Додати елемент у словник
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDict == null)
            {
                MessageBox.Show("Оберіть словник для додавання елемента.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string code = TxtItemCode.Text.Trim();
            string name = TxtItemName.Text.Trim();
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Код і назва елемента обов'язкові.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var it = new DictItem { DictId = selectedDict.DictId, Code = code, Name = name };
            Database.CreateItem(it);
            LoadItemsForSelectedDict();
        }

        // Оновити вибраний елемент
        private void BtnUpdateItem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == null)
            {
                MessageBox.Show("Оберіть елемент для оновлення.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string code = TxtItemCode.Text.Trim();
            string name = TxtItemName.Text.Trim();
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Код і назва елемента обов'язкові.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            selectedItem.Code = code;
            selectedItem.Name = name;
            Database.UpdateItem(selectedItem);
            LoadItemsForSelectedDict();
        }

        // Видалити вибраний елемент
        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == null)
            {
                MessageBox.Show("Оберіть елемент для видалення.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var res = MessageBox.Show($"Видалити елемент \"{selectedItem.Name}\"?",
                "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                Database.DeleteItem(selectedItem.ItemId);
                LoadItemsForSelectedDict();
            }
        }

        // Зміна вибраного елемента в таблиці
        private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedItem = ItemsGrid.SelectedItem as DictItem;
            if (selectedItem != null)
            {
                TxtItemCode.Text = selectedItem.Code;
                TxtItemName.Text = selectedItem.Name;
            }
            else
            {
                ClearItemEditFields();
            }
        }
    }
}