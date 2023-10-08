namespace Service.MangaOnline.ResponseModels;

public class HomeResponse
{
    public List<MangaResponse> TopMonthManga { get; set; }
    public List<MangaResponse> NewUpdateMangas { get; set; }
    public List<MangaResponse> NewDoneMangas { get; set; }
    public List<MangaResponse> TopViewMangas { get; set; }
}