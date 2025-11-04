import {Injectable} from '@angular/core';
import {HttpClient, HttpEvent, HttpEventType, HttpRequest} from '@angular/common/http';
import {Observable, map} from 'rxjs';

export interface TransactionImportOkResponse
{
    status: number;
    totalImportedLines: number;
    importedSummaryPerStores: Array<{ storeName: string; imported: number }>;
}

export interface TransactionImportErrorResponse
{
    status: number;
    totalInvalidLines: number;
    errors: Array<{ code: string; message: string }>;
}

export type TransactionImportMultiStatusResponse = { results: Array<TransactionImportOkResponse | TransactionImportErrorResponse> };

@Injectable({providedIn: 'root'})
export class TransactionService
{
    private baseUrl = '/api/v1/transactions';

    constructor(private http: HttpClient)
    {
    }

    importFile(file: File): Observable<TransactionImportOkResponse | TransactionImportErrorResponse | TransactionImportMultiStatusResponse>
    {
        const formData = new FormData();
        formData.append('file', file, file.name);

        return this.http.post<TransactionImportOkResponse | TransactionImportErrorResponse | TransactionImportMultiStatusResponse>(`${this.baseUrl}/import`, formData);
    }
}
