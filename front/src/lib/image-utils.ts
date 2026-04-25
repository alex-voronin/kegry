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

/**
 * Load an image from a URL and return the HTMLImageElement
 */
export const createImage = (url: string): Promise<HTMLImageElement> =>
  new Promise((resolve, reject) => {
    const image = new Image();
    image.addEventListener('load', () => resolve(image));
    image.addEventListener('error', (error) => reject(error));
    image.setAttribute('crossOrigin', 'anonymous');
    image.src = url;
  });

/**
 * Crop an image to the specified pixel area and return as a Blob.
 * Optionally supports flipping the image horizontally/vertically.
 * @param outputSize - If provided, the cropped image will be resized to this size.
 */
export async function getCroppedImg(
  imageSrc: string,
  pixelCrop: { x: number; y: number; width: number; height: number },
  flip = { horizontal: false, vertical: false },
  outputSize?: { width: number; height: number },
): Promise<Blob | null> {
  const image = await createImage(imageSrc);
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');

  if (!ctx) {
    return null;
  }

  // set canvas size to match the bounding box
  canvas.width = image.width;
  canvas.height = image.height;

  // translate canvas context to a central location to allow rotating and flipping around the center
  ctx.translate(image.width / 2, image.height / 2);
  ctx.scale(flip.horizontal ? -1 : 1, flip.vertical ? -1 : 1);
  ctx.translate(-image.width / 2, -image.height / 2);

  // draw image
  ctx.drawImage(image, 0, 0);

  const croppedCanvas = document.createElement('canvas');
  const croppedCtx = croppedCanvas.getContext('2d');

  if (!croppedCtx) {
    return null;
  }

  // Set the size of the output canvas (use outputSize if provided, otherwise use crop dimensions)
  const outWidth = outputSize?.width ?? pixelCrop.width;
  const outHeight = outputSize?.height ?? pixelCrop.height;
  croppedCanvas.width = outWidth;
  croppedCanvas.height = outHeight;

  // Draw the cropped image onto the new canvas, scaling to the output size
  croppedCtx.drawImage(
    canvas,
    pixelCrop.x,
    pixelCrop.y,
    pixelCrop.width,
    pixelCrop.height,
    0,
    0,
    outWidth,
    outHeight,
  );

  // As a blob
  return new Promise((resolve) => {
    const fileType = supportsWebP() ? 'image/webp' : 'image/jpeg';
    croppedCanvas.toBlob((file) => {
      resolve(file);
    }, fileType);
  });
}
