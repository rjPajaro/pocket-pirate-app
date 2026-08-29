import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  url = '';
  loading = false;
  error = '';
  progress = 0;
  status = '';
  elapsed = 0;
  toast = '';
  toastVisible = false;

  updateAvailable = false;
  updateDownloaded = false;
  updateDownloading = false;
  updateVersion = '';
  updateProgress = 0;

  private eventSource: EventSource | null = null;
  private timerInterval: ReturnType<typeof setInterval> | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;
  private startTime = 0;
  // Prevents onerror from treating a natural SSE close as a failure
  private jobDone = false;

  constructor(private http: HttpClient, private zone: NgZone) {}

  ngOnInit(): void {
    const updater = (window as any).updater;
    if (!updater) return;

    updater.onUpdateAvailable((info: any) => this.zone.run(() => {
      this.updateAvailable = true;
      this.updateVersion = info.version;
    }));

    updater.onDownloadProgress((p: any) => this.zone.run(() => {
      this.updateDownloading = true;
      this.updateProgress = Math.round(p.percent);
    }));

    updater.onUpdateDownloaded(() => this.zone.run(() => {
      this.updateDownloading = false;
      this.updateDownloaded = true;
    }));
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  installUpdate(): void {
    (window as any).updater?.installUpdate();
  }

  private get apiBase(): string {
    return (window as any).__API_PORT__
      ? `http://127.0.0.1:${(window as any).__API_PORT__}`
      : environment.apiUrl;
  }

  get elapsedDisplay(): string {
    const m = Math.floor(this.elapsed / 60);
    const s = this.elapsed % 60;
    return m > 0 ? `${m}:${s.toString().padStart(2, '0')}` : `${s}s`;
  }

  download(format: 'mp3' | 'mp4'): void {
    if (!this.url.trim()) {
      this.error = 'Please enter a URL.';
      return;
    }
    this.error = '';
    this.progress = 0;
    this.elapsed = 0;
    this.loading = true;
    this.jobDone = false;
    this.startTime = Date.now();

    this.timerInterval = setInterval(() => {
      this.elapsed = Math.floor((Date.now() - this.startTime) / 1000);
    }, 1000);

    this.http.post<{ jobId: string }>(
      `${this.apiBase}/api/mediaconverter/start-${format}`,
      JSON.stringify(this.url),
      { headers: { 'Content-Type': 'application/json' } }
    ).subscribe({
      next: ({ jobId }) => this.watchProgress(jobId, format),
      error: (err) => {
        this.resetState();
        this.error = `Failed to convert and download.`;
      }
    });
  }

  private watchProgress(jobId: string, format: string): void {
    this.eventSource = new EventSource(`${this.apiBase}/api/mediaconverter/progress/${jobId}`);

    // EventSource fires outside NgZone — wrap all state mutations so Angular detects changes
    this.eventSource.onmessage = (event) => {
      this.zone.run(() => {
        const data = JSON.parse(event.data);
        this.progress = data.progress ?? this.progress;
        this.status = data.status ?? '';

        if (data.status === 'done') {
          this.jobDone = true;
          this.progress = 100;
          this.cleanup();
          this.triggerDownload(jobId, format, data.fileName);
        } else if (data.status === 'error') {
          this.jobDone = true;
          this.cleanup();
          this.resetState();
          this.error = data.error || 'Conversion failed. Check the URL and try again.';
        }
      });
    };

    this.eventSource.onerror = () => {
      this.zone.run(() => {
        this.cleanup();
        if (!this.jobDone) {
          this.jobDone = true;
          this.triggerDownload(jobId, format);
        } else {
          this.resetState();
        }
      });
    };
  }

  private triggerDownload(jobId: string, format: string, fileName?: string): void {
    this.http.get(`${this.apiBase}/api/mediaconverter/download/${jobId}`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        // Prefer server-sent filename, fall back to Content-Disposition, then generic name
        let filename = fileName;
        if (!filename) {
          const cd = response.headers.get('Content-Disposition') ?? '';
          const match = cd.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          filename = match ? match[1].replace(/['"]/g, '') : `download.${format}`;
        }
        const blobUrl = URL.createObjectURL(response.body!);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(blobUrl);
        this.resetState();
        this.showToast(`Successfully Downloaded: ${filename}`);
      },
      error: (err) => {
        this.resetState();
        if (err?.status !== 404) {
          this.error = 'Download failed. Please try again.';
        }
        // 404 = job already consumed or not ready; UI still unblocked by resetState above
      }
    });
  }

  private showToast(message: string): void {
    this.toast = message;
    this.toastVisible = true;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => {
      this.toastVisible = false;
    }, 4000);
  }

  private resetState(): void {
    this.loading = false;
    this.progress = 0;
    this.status = '';
    this.elapsed = 0;
  }

  private cleanup(): void {
    this.eventSource?.close();
    this.eventSource = null;
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }
}
