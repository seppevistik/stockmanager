using AutoMapper;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;

namespace StockManager.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => src.CurrentStock * src.CostPerUnit));

        CreateMap<CreateProductDto, Product>();

        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductSKU, opt => opt.MapFrom(src => src.Product.SKU))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"));

        CreateMap<CreateStockMovementDto, StockMovement>();

        // Business mappings
        CreateMap<Business, BusinessDto>();
        CreateMap<UpdateBusinessDto, Business>();
    }
}
