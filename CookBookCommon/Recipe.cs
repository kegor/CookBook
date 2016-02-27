using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CookBookCommon
{
    public enum Category
    {
        Garnish = 1,
        [Display(Name = "Salad")]
        Salad = 2,
        [Display(Name = "Soup")]
        Soup = 3
    }
    public class Recipe
    {
        public int RecipeId { get; set; }
        [StringLength(100)]

        public string Title { get; set; }

        [DisplayName("Calories (kcals/100g)")]
        public int Calories { get; set; }
        
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }
        
        [StringLength(2083)]
        [DisplayName("Full-size Image")]
        public string ImageURL { get; set; }
        
        [StringLength(2083)]
        [DisplayName("Thumbnail")]
        public string ThumbnailURL { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [DisplayName("Addition Date")]
        public DateTime AdditionDate { get; set; }
        
        public Category? Category { get; set; }
        
        [StringLength(12)]
        [DisplayName("Cooking Time")]
        public string CookingTime { get; set; }
    }

}

