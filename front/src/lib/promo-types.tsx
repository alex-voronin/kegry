import {
  SmilePlus,
  Dices,
  Drama,
  GlassWater,
  DiamondPercent,
} from 'lucide-react';

export enum PromoType {
  General = 'general',
  HappyHours = 'happyHours',
  TwoPlusOne = 'twoPlusOne',
  Event = 'event',
  Discount = 'discount',
}

export const PromoIcon = ({
  type,
  className,
  size = 20,
}: {
  type: PromoType;
  className?: string;
  size?: number;
}) => {
  switch (type) {
    case PromoType.HappyHours:
      return <SmilePlus size={size} className={className} />;
    case PromoType.TwoPlusOne:
      return <Dices size={size} className={className} />;
    case PromoType.Event:
      return <Drama size={size} className={className} />;
    case PromoType.Discount:
      return <DiamondPercent size={size} className={className} />;
    case PromoType.General:
    default:
      return <GlassWater size={size} className={className} />;
  }
};
