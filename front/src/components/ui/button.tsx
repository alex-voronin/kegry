import * as React from "react"
import { cn } from "@/lib/utils"
import styles from "./button.module.css"

export enum ButtonVariant {
    Ghost = "ghost",
    Social = "social",
}

export enum ButtonSize {
    Default = "default",
    Xl = "xl",
}

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant
    size?: ButtonSize
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
    ({ className, variant = ButtonVariant.Ghost, size = ButtonSize.Default, ...props }, ref) => {
        return (
            <button
                ref={ref}
                className={cn(
                    styles.base,
                    {
                        // Variants
                        [styles.variantGhost]: variant === ButtonVariant.Ghost,
                        [styles.variantSocial]: variant === ButtonVariant.Social,

                        // Sizes
                        [styles.sizeDefault]: size === ButtonSize.Default,
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

