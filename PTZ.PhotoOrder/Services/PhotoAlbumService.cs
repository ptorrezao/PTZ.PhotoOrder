using Newtonsoft.Json;
using PTZ.PhotoOrder.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PTZ.PhotoOrder.Services
{
    public class PhotoAlbumService
    {
        private readonly PhotoOrderConfig photoOrderConfig;
        private readonly RestClient restClient;

        public PhotoAlbumService(PhotoOrderConfig photoOrderConfig)
        {
            this.photoOrderConfig = photoOrderConfig;
            this.restClient = new RestClient("https://api.trello.com/1/");

         
            CreateFolders(Path.Combine(Directory.GetCurrentDirectory()));
            CreateFolders(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            CreateFolders(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos"));
        }

        private static void CreateFolders(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        internal AlbumViewModel GetAlbum(int page, int pageSize)
        {
            var viewModel = new AlbumViewModel();

            viewModel.AlbumName = "Batizado Madalena";

            viewModel.Photos = this.GetPhotos(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos"), "*.jpg", SearchOption.TopDirectoryOnly), page, pageSize);

            return viewModel;
        }

        internal PhotoViewModel[] GetPhotos(int page, int pageSize)
        {
            string[] filenames = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos"), "*.jpg", SearchOption.TopDirectoryOnly);
            return this.GetPhotos(filenames, page, pageSize);
        }

        private PhotoViewModel[] GetPhotos(string[] filenames, int page, int pageSize)
        {
            List<PhotoViewModel> items = new List<PhotoViewModel>();
            foreach (var filename in filenames.Skip((page - 1) * pageSize).Take(pageSize))
            {
                var file = new FileInfo(filename);

                items.Add(new PhotoViewModel()
                {
                    Filename = file.Name,
                    RelativePath = $"/Photos/{file.Name}"
                });
            }

            return items.ToArray();
        }

        internal bool ValidateForm(EncomendaRequest form)
        {
            if (string.IsNullOrEmpty(form.Nome) ||
                string.IsNullOrEmpty(form.Email) ||
                string.IsNullOrEmpty(form.NumeroTelefone) ||
                form.Fotos?.Length <= 0)
            {
                return false;
            }

            return true;
        }


        internal void CreateOrderRequest(EncomendaRequest form)
        {
            var boardId = this.GetOrCreateBoard("Encomenda de Fotos " + this.photoOrderConfig.Title);

            var listId = this.GetOrCreateList(boardId, "A aguardar Pagamento");

            var cardId = this.CreateCard(listId, form);

            var checkListId = this.CreateChecklistId(cardId);

            foreach (var item in form.Fotos)
            {
                this.CreateChecklistitem(checkListId, item);
            }
        }

        private string CreateChecklistitem(string checkListId, string item)
        {
            var request = new RestRequest("checklists/{checkListId}/checkItems", Method.POST);
            request.AddParameter("checkListId", checkListId, ParameterType.UrlSegment);
            request.AddParameter("name", item, ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);

            var checklistItemId = result.id.ToString();
            return checklistItemId;
        }

        private string CreateChecklistId(string cardId)
        {
            var request = new RestRequest("checklists/", Method.POST);
            request.AddParameter("idCard", cardId, ParameterType.QueryString);
            request.AddParameter("name", "Fotos Encomendadas", ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);

            var checklistId = result.id.ToString();
            return checklistId;
        }

        private string CreateCard(string listId, EncomendaRequest form)
        {
            var title = $"{form.Nome} {form.Fotos.Count()} Fotos";
            var description = $"**Nome**: {form.Nome} {Environment.NewLine}" +
                $"**Email**: {form.Email} {Environment.NewLine}" +
                $"**Contacto**: {form.NumeroTelefone} {Environment.NewLine}" +
                $"**Forma Pagamento**: {form.PaymentRadio} {Environment.NewLine}" +
                $"**Total**: {form.Total}";

            var request = new RestRequest("cards/", Method.POST);
            request.AddParameter("name", title, ParameterType.QueryString);
            request.AddParameter("desc", description, ParameterType.QueryString);
            request.AddParameter("idList", listId, ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);

            var cardId = result.id.ToString();
            return cardId;
        }

        private string GetOrCreateBoard(string title)
        {
            var request = new RestRequest("members/{userId}/boards", Method.GET);
            request.AddParameter("userId", this.photoOrderConfig.Trello.Username, ParameterType.UrlSegment);
            request.AddParameter("filter", "open", ParameterType.QueryString);
            request.AddParameter("fields", "name", ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<List<dynamic>>(response.Content);

            if (result.Any(x => x.name == title))
            {
                return result.FirstOrDefault(x => x.name == title).id;
            }

            return this.CreateTrelloBoard(title);
        }

        private string CreateTrelloBoard(string title)
        {
            var request = new RestRequest("boards/", Method.POST);
            request.AddParameter("name", title, ParameterType.QueryString);
            request.AddParameter("defaultLists", "false", ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            var boardId = result.id.ToString();

            this.CreateTrelloBoardList(boardId, "Entregue");
            this.CreateTrelloBoardList(boardId, "Encomendado");
            this.CreateTrelloBoardList(boardId, "Pago");
            this.CreateTrelloBoardList(boardId, "A aguardar Pagamento");

            return boardId;
        }

        private string CreateTrelloBoardList(string idBoard, string name)
        {
            var request = new RestRequest("lists/", Method.POST);
            request.AddParameter("name", name, ParameterType.QueryString);
            request.AddParameter("idBoard", idBoard, ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);

            return result.id.ToString();
        }

        private string GetOrCreateList(string boardId, string name)
        {
            var request = new RestRequest("/boards/{id}/lists", Method.GET);
            request.AddParameter("id", boardId, ParameterType.UrlSegment);
            request.AddParameter("fields", "name", ParameterType.QueryString);
            request.AddParameter("key", this.photoOrderConfig.Trello.APIKey, ParameterType.QueryString);
            request.AddParameter("token", this.photoOrderConfig.Trello.Token, ParameterType.QueryString);
            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<List<dynamic>>(response.Content);

            if (result.Any(x => x.name == name))
            {
                return result.FirstOrDefault(x => x.name == name).id;
            }

            return this.CreateTrelloBoardList(boardId, name);
        }
    }
}
