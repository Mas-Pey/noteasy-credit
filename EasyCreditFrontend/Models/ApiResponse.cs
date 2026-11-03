using System;

namespace EasyCreditFrontend.Models;

// Envelope standar EasyCredit API: { status, message, data }
public class ApiResponse<T>
{
    public bool Status {get; set;}
    public string Message {get; set;} = String.Empty;
    public T? Data {get; set;}

    public bool IsSuccess => Status;
}

// Bentuk data untuk GET /api/customers: { page, size, total, items }
public class PagedData<T>
{
    public int Page{get; set;}
    public int Size{get;set;}
    public int  Total{get;set;}
    public List<T> Items {get; set;} = new();
}

// Bentuk data untuk GET /api/credit-applications: { total, items }
public class TotalItemsData<T>
{
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

