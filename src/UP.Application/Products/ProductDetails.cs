using UP.Application.Images;
using UP.Domain.Products;

namespace UP.Application.Products;

public sealed record ProductDetails(Product Product, IReadOnlyList<PublicImage> Images);
