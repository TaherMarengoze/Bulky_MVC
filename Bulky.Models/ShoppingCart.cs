using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models;

public class ShoppingCart
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    [ValidateNever]
    public Product Product { get; set; }

    [Range(0, 1000, ErrorMessage = "Please, enter a value between 1 and 1000")]
    public int Count { get; set; }

    public string ApplicationUserId { get; set; }
    [ForeignKey(nameof(ApplicationUserId))]
    [ValidateNever]
    public ApplicationUser ApplicationUser { get; set; }

    [NotMapped]
    public double Price { get; set; }
}
