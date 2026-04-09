import axios from 'axios';
import apiClient from '@/lib/api-client';
import imageCompression from 'browser-image-compression';

const uploadClient = axios.create({
  baseURL: '',
  headers: {},
});

export async function compressImageToBlob(
  imageSource: string | Blob | File,
  maxSizeBytes: number = 512 * 1024,
  maxLongSide: number = 1920,
): Promise<Blob> {
  let file: File;

  if (typeof imageSource === 'string') {
    const response = await fetch(imageSource, { mode: 'cors' });
    const blob = await response.blob();
    file = new File([blob], 'image.jpg', { type: blob.type || 'image/jpeg' });
  } else if (imageSource instanceof File) {
    file = imageSource;
  } else {
    // Blob to File
    file = new File([imageSource], 'image.jpg', {
      type: imageSource.type || 'image/jpeg',
    });
  }

  const options = {
    maxSizeMB: maxSizeBytes / (1024 * 1024),
    maxWidthOrHeight: maxLongSide,
    useWebWorker: false,
    fileType: supportsWebP() ? 'image/webp' : 'image/jpeg',
    initialQuality: 0.85,
    preserveExif: false,
    libURL: undefined,
  };

  const compressedFile = await imageCompression(file, options);
  return compressedFile;
}

function supportsWebP(): boolean {
  const canvas = document.createElement('canvas');
  return canvas.toDataURL('image/webp').startsWith('data:image/webp');
}

/**
 * Create an Object URL from a Blob for display in img elements
 * Remember to revoke the URL when done using URL.revokeObjectURL()
 */
export function createImageUrl(blob: Blob): string {
  return URL.createObjectURL(blob);
}

/**
 * Upload an image blob to S3 using a presigned URL endpoint
 * Returns the public download URL of the uploaded image
 */
export async function uploadImage(
  blob: Blob,
  presignedUrlEndpoint: string,
): Promise<string> {
  interface PresignedUrlResponse {
    uploadUrl: string;
    publicDownloadUrl: string;
  }

  // Get presigned URL
  const presignedResponse = await apiClient.post<{
    success: boolean;
    result: PresignedUrlResponse;
    errors: { error: string }[];
  }>(presignedUrlEndpoint, {
    ContentType: blob.type === 'image/jpeg' ? 'ImageJpeg' : 'ImageWebp',
    ContentLength: blob.size,
  });

  if (!presignedResponse.data.success) {
    throw new Error(
      presignedResponse.data.errors[0]?.error || 'Failed to get upload URL',
    );
  }

  const { uploadUrl, publicDownloadUrl } = presignedResponse.data.result;

  // Upload to S3
  await uploadClient.put(uploadUrl, blob, {
    headers: {
      'Content-Type': blob.type || 'image/webp',
    },
  });

  return publicDownloadUrl;
}
