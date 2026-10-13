using FashionStore.Models;

namespace FashionStore.ViewModels;

public class AdminProductVariantsViewModel
{
    public Product Product { get; init; } = null!;
    public List<ProductVariant> Variants { get; init; } = [];
    public List<Size> Sizes { get; init; } = [];
    public List<Color> Colors { get; init; } = [];
}
