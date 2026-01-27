'use client';

const METERS_PER_MILE = 1609.34;
const METERS_PER_KM = 1000;

export function metersToKilometers(meters: number): number {
  return Math.round(meters / METERS_PER_KM);
}

export function metersToMiles(meters: number): number {
  return Math.round(meters / METERS_PER_MILE);
}

export function kilometersToMeters(kilometers: number): number {
  return kilometers * METERS_PER_KM;
}

export function milesToMeters(miles: number): number {
  return miles * METERS_PER_MILE;
}
