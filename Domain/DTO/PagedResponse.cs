using Newtonsoft.Json;

namespace PracticeApi.Domain.DTO
{
    public class PagedResponse<T> : GeneralResponse<T>
    {

        public PagedResponse(T data, int pageNumber, int pageSize, int totalRecords, string message)
        {
            this.Succeeded = true;
            this.Code = 200;
            this.Message = message;
            this.Data = data;
            this.Errors = null;
            this.PageMeta = new PageMeta(pageNumber, pageSize, totalRecords);
        }
    }
    public class PageMeta
    {
        public PageMeta(int pageNumber, int pageSize, int totalRecords)
        {
            this.PageNumber = pageNumber;
            this.PageSize = pageSize;
            this.TotalRecords = totalRecords;
            this.TotalPages = calculateTotalPages(totalRecords, pageSize);
        }
        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
        [JsonProperty("totalRecords")]
        public int TotalRecords { get; set; }

        private int calculateTotalPages(int totalRecords, int pageSize)
        {
            var totalPages = totalRecords / pageSize;

            if (totalRecords % pageSize > 0)
                totalPages++;
            return totalPages;
        }
    }
}
