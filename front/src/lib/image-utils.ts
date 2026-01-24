/**
 * Resize an image to specific dimensions and return as Blob
 */
export function resizeImageToBlob(
  imageSource: string | Blob,
  targetWidth: number,
  targetHeight: number,
  quality = 0.9,
): Promise<Blob> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    const src =
      typeof imageSource === 'string'
        ? imageSource
        : URL.createObjectURL(imageSource);

    image.src = src;
    image.onload = () => {
      // Revoke object URL if we created one
      if (typeof imageSource !== 'string') {
        URL.revokeObjectURL(src);
      }

      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) return reject('No canvas context');

      canvas.width = targetWidth;
      canvas.height = targetHeight;

      ctx.drawImage(image, 0, 0, targetWidth, targetHeight);

      canvas.toBlob(
        (blob) => {
          if (blob) {
            resolve(blob);
          } else {
            reject('Failed to create blob');
          }
        },
        'image/jpeg',
        quality,
      );
    };
    image.onerror = reject;
  });
}

/**
 * Compress an image to be under a target file size (in bytes)
 * Uses iterative quality reduction and returns a Blob
 */
export async function compressImageToBlob(
  imageSource: string | Blob,
  maxSizeBytes: number,
  maxWidth?: number,
): Promise<Blob> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    const src =
      typeof imageSource === 'string'
        ? imageSource
        : URL.createObjectURL(imageSource);

    image.src = src;
    image.onload = () => {
      // Revoke object URL if we created one
      if (typeof imageSource !== 'string') {
        URL.revokeObjectURL(src);
      }

      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) return reject('No canvas context');

      // Calculate dimensions (optionally limit max width while preserving aspect ratio)
      let width = image.width;
      let height = image.height;

      if (maxWidth && width > maxWidth) {
        const ratio = maxWidth / width;
        width = maxWidth;
        height = Math.round(height * ratio);
      }

      canvas.width = width;
      canvas.height = height;
      ctx.drawImage(image, 0, 0, width, height);

      // Try different quality levels to get under maxSizeBytes
      const tryCompress = (quality: number) => {
        canvas.toBlob(
          (blob) => {
            if (!blob) {
              reject('Failed to create blob');
              return;
            }

            if (blob.size <= maxSizeBytes || quality <= 0.1) {
              // If still too large at min quality, reduce dimensions
              if (blob.size > maxSizeBytes) {
                const scaleFactor = Math.sqrt(maxSizeBytes / blob.size);
                canvas.width = Math.round(width * scaleFactor);
                canvas.height = Math.round(height * scaleFactor);
                ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
                canvas.toBlob(
                  (finalBlob) => {
                    if (finalBlob) {
                      resolve(finalBlob);
                    } else {
                      reject('Failed to create final blob');
                    }
                  },
                  'image/jpeg',
                  0.8,
                );
              } else {
                resolve(blob);
              }
            } else {
              // Try lower quality
              tryCompress(quality - 0.1);
            }
          },
          'image/jpeg',
          quality,
        );
      };

      tryCompress(0.9);
    };
    image.onerror = reject;
  });
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
  const axios = (await import('axios')).default;

  interface PresignedUrlResponse {
    uploadUrl: string;
    publicDownloadUrl: string;
  }

  // Get presigned URL
  const presignedResponse = await axios.post<{
    success: boolean;
    result: PresignedUrlResponse;
    errors: { error: string }[];
  }>(presignedUrlEndpoint, { ContentLength: blob.size });

  if (!presignedResponse.data.success) {
    throw new Error(
      presignedResponse.data.errors[0]?.error || 'Failed to get upload URL',
    );
  }

  const { uploadUrl, publicDownloadUrl } = presignedResponse.data.result;

  // Upload to S3
  await axios.put(uploadUrl, blob, {
    headers: {
      'Content-Type': blob.type || 'image/jpeg',
    },
  });

  return publicDownloadUrl;
}
