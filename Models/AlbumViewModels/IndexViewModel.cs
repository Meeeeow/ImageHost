using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models.AlbumViewModels
{
    public class IndexViewModel
    {
        public AlbumListViewModel AlbumListViewModel { get; set; }
        public AlbumCreateViewModel AlbumCreateViewModel { get; set; }
    }

    public class AlbumListViewModel
    {
        public List<Album> Albums;
    }

    public class AlbumCreateViewModel
    {
        [Required]
        [Display(Name = "Album name")]
        public string AlbumName { get; set; }

        [Required]
        public string Visibility { get; set; }
    }
}