import {Component} from '@angular/core';
import {TransactionService, TransactionImportErrorResponse, TransactionImportOkResponse, TransactionImportMultiStatusResponse} from '../transaction.service';

@Component({
    standalone: false,
    selector: 'app-transaction-import',
    templateUrl: './transaction-import.component.html',
    styleUrls: ['./transaction-import.component.css']
})
export class TransactionImportComponent
{
    fileToUpload: File | null = null;
    isUploading = false;
    hasCompletedImport = false;
    importSummary: TransactionImportOkResponse | null = null;
    validationErrors: Array<{ code: string; message: string }> = [];

    constructor(private transactionService: TransactionService)
    {
    }

    onFileChange(event: Event): void
    {
        const input = event.target as HTMLInputElement;

        if (input?.files && input.files.length > 0)
        {
            this.fileToUpload = input.files[0];
        }
        else
        {
            this.fileToUpload = null;
        }
    }

    onImport(): void
    {
        if (!this.fileToUpload || this.isUploading)
        {
            return;
        }

        this.isUploading = true;
        this.hasCompletedImport = false;
        this.importSummary = null;
        this.validationErrors = [];

        this.transactionService.importFile(this.fileToUpload).subscribe({
            next: (res) =>
            {
                const parsed = this.parseImportResponse(res);
                this.importSummary = parsed.okResponse;
                this.validationErrors = parsed.errors;
                this.hasCompletedImport = true;
            },
            error: (ex) =>
            {
                console.error(ex);
                this.isUploading = false;
            },
            complete: () =>
            {
                this.isUploading = false;
            }
        });
    }

    private parseImportResponse(response: TransactionImportOkResponse | TransactionImportErrorResponse | TransactionImportMultiStatusResponse): {
        okResponse: TransactionImportOkResponse | null;
        errors: Array<{ code: string; message: string }>
    }
    {
        var okResponse: TransactionImportOkResponse | null = null;
        var errors: Array<{ code: string; message: string }> = [];

        if ((response as TransactionImportMultiStatusResponse).results)
        {
            const multi = response as TransactionImportMultiStatusResponse;
            multi.results.forEach(r =>
            {
                if ((r as TransactionImportOkResponse).importedSummaryPerStores)
                {
                    okResponse = r as TransactionImportOkResponse;
                }
                else
                {
                    const err = r as TransactionImportErrorResponse;
                    errors = err.errors || [];
                }
            });
        }
        else if ((response as TransactionImportOkResponse).importedSummaryPerStores)
        {
            okResponse = response as TransactionImportOkResponse;
        }
        else
        {
            const err = response as TransactionImportErrorResponse;
            errors = err.errors || [];
        }

        return {okResponse, errors};
    }
}
