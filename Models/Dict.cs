using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryApp.Models
{
    // Модель словника (таблиця DICT)
    public class Dict
    {
        // Первинний ключ
        public int DictId { get; set; }

        // Посилання на батьківський словник
        public int? ParentId { get; set; }

        // Назва словника
        public string Name { get; set; } = "";

        // Код словника
        public string Code { get; set; } = "";

        // Опис словника
        public string? Description { get; set; }
    }
}
