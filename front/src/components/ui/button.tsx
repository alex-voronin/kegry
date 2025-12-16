import * as React from "react"
import { cn } from "@/lib/utils"
import styles from "./button.module.css"

export enum ButtonVariant {
    Default = "default",
    Outline = "outline",
    Ghost = "ghost",
    Social = "social",
    Link = "link",
}

export enum ButtonSize {
    Default = "default",
    Sm = "sm",
    Lg = "lg",
    Icon = "icon",
    Xl = "xl",
}

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant
    size?: ButtonSize
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
    ({ className, variant = ButtonVariant.Default, size = ButtonSize.Default, ...props }, ref) => {
        return (
            <button
                ref={ref}
                className={cn(
                    styles.base,
                    {
                        // Variants
                        [styles.variantDefault]: variant === ButtonVariant.Default,
                        [styles.variantOutline]: variant === ButtonVariant.Outline,
                        [styles.variantGhost]: variant === ButtonVariant.Ghost,
                        [styles.variantLink]: variant === ButtonVariant.Link,
                        [styles.variantSocial]: variant === ButtonVariant.Social,

                        // Sizes
                        [styles.sizeDefault]: size === ButtonSize.Default,
                        [styles.sizeSm]: size === ButtonSize.Sm,
                        [styles.sizeLg]: size === ButtonSize.Lg,
                        [styles.sizeIcon]: size === ButtonSize.Icon,
                        [styles.sizeXl]: size === ButtonSize.Xl,
                    },
                    className
                )}
                {...props}
            />
        )
    }
)
Button.displayName = "Button"

export { Button }
