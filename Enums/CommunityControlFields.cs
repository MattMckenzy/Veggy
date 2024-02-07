namespace Veggy.Enums;

public enum CommunityControlFields
{
    Id,
    [Display(Name = "Origin Url")]
    OriginUrl,
    [Display(Name = "Post Fetch Count")]
    PostFetchCount,
    Name,
    Title,
    Description,
    [Display(Name = "Icon Url")]
    IconUrl,
    [Display(Name = "Banner Url")]
    BannerUrl,
    IsNSFW
}