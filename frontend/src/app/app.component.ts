import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  url = '';
  loading = false;
  error = '';

  constructor(private http: HttpClient) {}

  download(format: 'mp3' | 'mp4'): void {
    if (!this.url.trim()) {
      this.error = 'Please enter a URL.';
      return;
    }
    this.error = '';
    this.loading = true;
    const endpoint = `https://localhost:7208/api/mediaconverter/convert-to-${format}`;
    this.http
      .post(endpoint, JSON.stringify(this.url), {
        headers: { 'Content-Type': 'application/json' },
        responseType: 'blob',
        observe: 'response',
      })
      .subscribe({
        next: (response) => {
          this.loading = false;
          const contentDisposition = response.headers.get('Content-Disposition') ?? '';
          const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          const filename = match ? match[1].replace(/['"]/g, '') : `download.${format}`;
          const blob = response.body!;
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = filename;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: (err) => {
          this.loading = false;
          const msg = err?.message ?? '';
          this.error = `Failed to convert to ${format.toUpperCase()}. ${msg || 'Check the URL and try again.'}`;
        },
      });
  }
}
