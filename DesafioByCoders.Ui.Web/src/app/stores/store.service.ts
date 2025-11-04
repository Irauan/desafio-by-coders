import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';

export interface StoreListItem
{
    id: number;
    name: string;
    owner: string;
    balance: number;
}

@Injectable({providedIn: 'root'})
export class StoreService
{
    private baseUrl = '/api/v1/stores';

    constructor(private http: HttpClient)
    {
    }

    list(): Observable<StoreListItem[]>
    {
        return this.http.get<StoreListItem[]>(this.baseUrl);
    }
}
