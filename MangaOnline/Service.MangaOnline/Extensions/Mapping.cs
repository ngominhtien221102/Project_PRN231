﻿using Service.MangaOnline.Models;
using Service.MangaOnline.ResponseModels;
using System;

namespace Service.MangaOnline.Extensions;

public class Mapping : IMapping
{
    public MangaResponse MapMangaResponse(Manga manga)
    {
        return new MangaResponse
        {
            Id = manga.Id,
            AuthorId = manga.AuthorId,
            Status = manga.Status,
            ViewCount = manga.ViewCount,
            RateCount = manga.RateCount,
            Star = manga.Star,
            FollowCount = manga.FollowCount,
            Description = manga.Description,
            CreatedAt = manga.CreatedAt,
            ModifiedAt = manga.ModifiedAt,
            IsActive = manga.IsActive,
            Image = manga.Image,
            Name = manga.Name,
            AuthorName = manga.Author.Name,
            CategoryMangas = manga.CategoryMangas
                .Select(x => new Category
                {
                    Id = x.CategoryId,
                    Name = x.Category.Name,
                    SubId = x.Category.SubId
                }).ToList(),
            Chapteres = manga.Chapteres.Select(MapChapterResponse).ToList(),
            CountChapter = manga.Chapteres.ToList().Count
        };
    }

    public ChapterResponse MapChapterResponse(Chaptere manga)
    {
        return new ChapterResponse
        {
            Id = manga.Id,
            SubId = manga.SubId,
            MangaId = manga.MangaId,
            ChapterNumber = manga.ChapterNumber,
            Name = manga.Name,
            CreatedAt = manga.CreatedAt,
            Status = manga.Status,
            IsActive = manga.IsActive,
            FilePdf = manga.FilePdf
        };
    }

    public UserResponse MapUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            Avatar = user.Avatar,
            RoleId = user.RoleId,
            UserToken =  new UserToken
                {
                    Id = user.UserToken.Id,
                    UserId = user.Id,
                    Email = user.Email,
                    Expires = user.UserToken.Expires,
                    Value= user.UserToken.Value
                }
        };
    }
}