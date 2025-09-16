using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryApp.Models
{
    // Модель елемента словника (таблиця DICT_ITEM)
    public class DictItem
    {
        // Первинний ключ
        public int ItemId { get; set; }

        // Ідентифікатор словника, до якого належить елемент
        public int DictId { get; set; }

        // Код елемента
        public string Code { get; set; } = "";

        // Назва елемента
        public string Name { get; set; } = "";
    }
}
